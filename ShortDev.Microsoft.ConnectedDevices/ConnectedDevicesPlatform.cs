using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using ShortDev.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices;

public sealed class ConnectedDevicesPlatform : IDisposable
{
    public LocalDeviceInfo DeviceInfo { get; }

    readonly ILogger<ConnectedDevicesPlatform> _logger;
    public ConnectedDevicesPlatform(LocalDeviceInfo deviceInfo)
    {
        DeviceInfo = deviceInfo;
        _logger = deviceInfo.LoggerFactory.CreateLogger<ConnectedDevicesPlatform>();
    }

    #region Transport
    readonly ConcurrentDictionary<Type, ICdpTransport> _transports = new();
    public void AddTransport<T>(T transport) where T : ICdpTransport
    {
        _transports.AddOrUpdate(typeof(T), transport, (_, old) =>
        {
            old.Dispose();
            return transport;
        });
    }

    public T? TryGetTransport<T>() where T : ICdpTransport
        => (T?)_transports.GetValueOrDefault(typeof(T));

    public ICdpTransport? TryGetTransport(CdpTransportType transportType)
        => _transports.Values.SingleOrDefault(transport => transport.TransportType == transportType);
    #endregion

    #region Host
    #region Advertise
    public bool IsAdvertising { get; private set; } = false;
    public void Advertise(CancellationToken cancellationToken)
    {
        lock (this)
        {
            if (IsAdvertising)
                return;

            IsAdvertising = true;
        }

        _logger.LogDebug("Startet advertising");

        foreach (var (_, transport) in _transports)
            if (transport is ICdpDiscoverableTransport discoverableTransport)
                discoverableTransport.Advertise(DeviceInfo, cancellationToken);

        cancellationToken.Register(() =>
        {
            lock (this)
                IsAdvertising = false;
        });
    }
    #endregion

    #region Listen
    public bool IsListening { get; private set; } = false;
    public void Listen(CancellationToken cancellationToken)
    {
        lock (this)
        {
            if (IsListening)
                return;

            IsListening = true;
        }

        _logger.LogDebug("Startet listening");

        foreach (var (_, transport) in _transports)
        {
            transport.Listen(cancellationToken);
            transport.DeviceConnected += OnDeviceConnected;
        }

        cancellationToken.Register(() =>
        {
            lock (this)
                IsListening = false;
        });
    }

    private void OnDeviceConnected(ICdpTransport sender, CdpSocket socket)
    {
        _logger.Log(LogLevel.Information, "Device {0} ({1}) connected via {2}", socket.RemoteDevice.Name, socket.RemoteDevice.Endpoint.Address, socket.TransportType);
        ReceiveLoop(socket);
    }
    #endregion
    #endregion

    #region Client
    public void Discover(CancellationToken cancellationToken)
    {
        foreach (var (_, transport) in _transports)
        {
            if (transport is ICdpDiscoverableTransport discoverableTransport)
            {
                discoverableTransport.Discover(cancellationToken);
                discoverableTransport.DeviceDiscovered += DeviceDiscovered;
            }
        }
    }

    public event DeviceDiscoveredEventHandler? DeviceDiscovered;

    public async Task<CdpSession> ConnectAsync(CdpDevice device)
    {
        var socket = await CreateSocketAsync(device);
        return await CdpSession.CreateClientAndConnectAsync(this, socket);
    }

    internal async Task<CdpSocket> CreateSocketAsync(CdpDevice device)
    {
        if (TryGetKnownSocket(device.Endpoint, out var knownSocket))
            return knownSocket;

        var transport = TryGetTransport(device.Endpoint.TransportType) ?? throw new InvalidOperationException($"No single transport found for type {device.Endpoint.TransportType}");
        var socket = await transport.ConnectAsync(device);
        ReceiveLoop(socket);
        return socket;
    }

    internal async Task<CdpSocket?> TryCreateSocketAsync(CdpDevice device, TimeSpan connectTimeout)
    {
        if (TryGetKnownSocket(device.Endpoint, out var knownSocket))
            return knownSocket;

        var transport = TryGetTransport(device.Endpoint.TransportType);
        if (transport == null)
            return null;

        var socket = await transport.TryConnectAsync(device, connectTimeout);
        if (socket == null)
            return null;

        ReceiveLoop(socket);
        return socket;
    }
    #endregion

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
                        var header = CommonHeader.Parse(streamReader);

                        if (socket.IsClosed)
                            return;

                        session = CdpSession.GetOrCreate(
                            this,
                            socket.RemoteDevice ?? throw new InvalidDataException(),
                            header
                        );

                        var payload = streamReader.ReadBytes(header.PayloadSize);

                        if (socket.IsClosed)
                            return;

                        session.HandleMessage(socket, header, new EndianReader(Endianness.BigEndian, payload));
                    }
                    catch (Exception ex)
                    {
                        if (socket.IsClosed)
                            return;

                        _logger.Log(LogLevel.Warning, "{0} in session {1} \n {2}",
                            ex.GetType().Name,
                            session?.LocalSessionId.ToString() ?? "null",
                            ex
                        );
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
            _knownSockets.TryRemove(socket.RemoteDevice.Endpoint, out _); // ToDo: We might remove a newer socket here!!
            socket.Disposed -= OnSocketClosed;
        }

        _knownSockets.AddOrUpdate(socket.RemoteDevice.Endpoint, socket, (key, current) =>
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
        => DeviceInfo.ToCdpDeviceInfo(_transports.Select(x => x.Value.GetEndpoint()).ToArray());

    public void Dispose()
    {
        foreach (var (_, transport) in _transports)
            transport.Dispose();
    }

    public static X509Certificate2 CreateDeviceCertificate(CdpEncryptionParams encryptionParams)
    {
        CertificateRequest certRequest = new("CN=Ms-Cdp", ECDsa.Create(encryptionParams.Curve), HashAlgorithmName.SHA256);
        return certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
    }

    public static ILoggerFactory CreateLoggerFactory(Action<string> messageCallback)
        => LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();

            BasicLoggingProvider provider = new();
            provider.MessageReceived += messageCallback;
            builder.AddProvider(provider);

            builder.SetMinimumLevel(LogLevel.Debug);
        });
}
