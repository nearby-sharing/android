using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ShortDev.Microsoft.ConnectedDevices;

public sealed class ConnectedDevicesPlatform(LocalDeviceInfo deviceInfo, ILoggerFactory loggerFactory) : IDisposable
{
    public LocalDeviceInfo DeviceInfo { get; } = deviceInfo;

    readonly ILogger<ConnectedDevicesPlatform> _logger = loggerFactory.CreateLogger<ConnectedDevicesPlatform>();

    #region Transport
    readonly ConcurrentDictionary<Type, ICdpTransport> _transportMap = new();
    public void AddTransport<T>(T transport) where T : ICdpTransport
    {
        _transportMap.AddOrUpdate(typeof(T), transport, (_, old) =>
        {
            old.Dispose();
            return transport;
        });
    }

    public T? TryGetTransport<T>() where T : ICdpTransport
        => (T?)_transportMap.GetValueOrDefault(typeof(T));

    public ICdpTransport? TryGetTransport(CdpTransportType transportType)
        => _transportMap.Values.SingleOrDefault(transport => transport.TransportType == transportType);
    #endregion

    #region Host
    #region Advertise
    public GuardFlag IsAdvertising { get; } = new();
    public async void Advertise(CancellationToken cancellationToken)
    {
        using var isAdvertising = IsAdvertising.Lock();

        _logger.AdvertisingStarted();
        try
        {
            await Task.WhenAll(_transportMap.Values
                .OfType<ICdpDiscoverableTransport>()
                .Select(x => x.Advertise(DeviceInfo, cancellationToken))
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.AdvertisingError(ex);
        }
        finally
        {
            _logger.AdvertisingStopped();
        }
    }
    #endregion

    #region Listen
    public GuardFlag IsListening { get; } = new();
    public async void Listen(CancellationToken cancellationToken)
    {
        using var isListening = IsListening.Lock();

        _logger.ListeningStarted();
        try
        {
            await Task.WhenAll(_transportMap.Values
                .Select(async transport =>
                {
                    transport.DeviceConnected += OnDeviceConnected;
                    try
                    {
                        await transport.Listen(cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        transport.DeviceConnected -= OnDeviceConnected;
                    }
                })
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.ListeningError(ex);
        }
        finally
        {
            _logger.ListeningStopped();
        }
    }

    private void OnDeviceConnected(ICdpTransport sender, CdpSocket socket)
    {
        _logger.NewSocket(socket.Endpoint);
        ReceiveLoop(socket);
    }
    #endregion
    #endregion

    #region Client
    public event DeviceDiscoveredEventHandler? DeviceDiscovered;

    public GuardFlag IsDiscovering { get; } = new();
    public async void Discover(CancellationToken cancellationToken)
    {
        using var isDiscovering = IsDiscovering.Lock();

        _logger.DiscoveryStarted(_transportMap.Values.Select(x => x.TransportType));
        try
        {
            await Task.WhenAll(_transportMap.Values
                .OfType<ICdpDiscoverableTransport>()
                .Select(async transport =>
                {
                    transport.DeviceDiscovered += DeviceDiscovered;
                    try
                    {
                        await transport.Discover(cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        transport.DeviceDiscovered -= DeviceDiscovered;
                    }
                })
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.DiscoveryError(ex);
        }
        finally
        {
            _logger.DiscoveryStopped();
        }
    }

    public async Task<CdpSession> ConnectAsync([NotNull] EndpointInfo endpoint)
    {
        var socket = await CreateSocketAsync(endpoint).ConfigureAwait(false);
        return await CdpSession.ConnectClientAsync(this, socket).ConfigureAwait(false);
    }

    internal async Task<CdpSocket> CreateSocketAsync(EndpointInfo endpoint)
    {
        if (TryGetKnownSocket(endpoint, out var knownSocket))
            return knownSocket;

        var transport = TryGetTransport(endpoint.TransportType) ?? throw new InvalidOperationException($"No single transport found for type {endpoint.TransportType}");
        var socket = await transport.ConnectAsync(endpoint).ConfigureAwait(false);
        ReceiveLoop(socket);
        return socket;
    }

    internal async Task<CdpSocket?> TryCreateSocketAsync(EndpointInfo endpoint, TimeSpan connectTimeout)
    {
        if (TryGetKnownSocket(endpoint, out var knownSocket))
            return knownSocket;

        var transport = TryGetTransport(endpoint.TransportType);
        if (transport == null)
            return null;

        var socket = await transport.TryConnectAsync(endpoint, connectTimeout).ConfigureAwait(false);
        if (socket == null)
            return null;

        ReceiveLoop(socket);
        return socket;
    }
    #endregion

    static readonly ArrayPool<byte> _messagePool = ArrayPool<byte>.Create();
    private void ReceiveLoop(CdpSocket socket)
    {
        RegisterKnownSocket(socket);
        Task.Run(() =>
        {
            EndianReader streamReader = new(Endianness.BigEndian, socket.InputStream);
            using (socket)
            {
                do
                {
                    CdpSession? session = null;
                    try
                    {
                        var header = CommonHeader.Parse(ref streamReader);

                        if (socket.IsClosed)
                            return;

                        session = CdpSession.GetOrCreate(
                            this,
                            socket.Endpoint,
                            header
                        );

                        using var payload = _messagePool.RentToken(header.PayloadSize);
                        streamReader.ReadBytes(payload.Span);

                        if (socket.IsClosed)
                            return;

                        EndianReader reader = new(Endianness.BigEndian, payload.Span);
                        session.HandleMessage(socket, header, ref reader);
                    }
                    catch (Exception ex)
                    {
                        if (socket.IsClosed)
                            return;

                        if (session != null)
                            _logger.ExceptionInSession(ex, session.SessionId.AsNumber());
                        else
                            _logger.ExceptionInReceiveLoop(ex, socket.TransportType);

                        break;
                    }
                } while (!socket.IsClosed);
            }
        });
    }

    #region Socket Management
    readonly ConcurrentDictionary<EndpointInfo, CdpSocket> _knownSockets = new();

    void RegisterKnownSocket(CdpSocket socket)
    {
        socket.Disposed += OnSocketClosed;
        void OnSocketClosed()
        {
            _knownSockets.TryRemove(socket.Endpoint, out _); // ToDo: We might remove a newer socket here!!
            socket.Disposed -= OnSocketClosed;
        }

        _knownSockets.AddOrUpdate(socket.Endpoint, socket, (key, current) =>
        {
            // ToDo: Alive check
            return socket;
        });
    }

    bool TryGetKnownSocket(EndpointInfo endpoint, [MaybeNullWhen(false)] out CdpSocket socket)
    {
        if (!_knownSockets.TryGetValue(endpoint, out socket))
            return false;

        // ToDo: Alive check!!
        if (socket.IsClosed)
            return false;

        return true;
    }
    #endregion

    public CdpDeviceInfo GetCdpDeviceInfo()
    {
        List<EndpointInfo> endpoints = [];
        foreach (var (_, transport) in _transportMap)
        {
            try
            {
                var endpoint = transport.GetEndpoint();
                endpoints.Add(endpoint);
            }
            catch { }
        }
        return DeviceInfo.ToCdpDeviceInfo(endpoints);
    }

    public ILogger<T> CreateLogger<T>()
        => loggerFactory.CreateLogger<T>();

    public void Dispose()
    {
        Extensions.DisposeAll(
            _transportMap.Select(x => x.Value),
            _knownSockets.Select(x => x.Value)
        );

        _transportMap.Clear();
        _knownSockets.Clear();
    }

    public static X509Certificate2 CreateDeviceCertificate([NotNull] CdpEncryptionParams encryptionParams)
    {
        using var key = ECDsa.Create(encryptionParams.Curve);
        CertificateRequest certRequest = new("CN=Ms-Cdp", key, HashAlgorithmName.SHA256);
        return certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
    }

    public static ILoggerFactory CreateLoggerFactory(string filePattern, LogLevel logLevel = LogLevel.Debug)
        => LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();

            builder.SetMinimumLevel(logLevel);

            builder.AddFile(filePattern, logLevel);
        });
}
