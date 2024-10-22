using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Internal;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Session.Channels;
using ShortDev.Microsoft.ConnectedDevices.Session.Connection;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Collections.Concurrent;

namespace ShortDev.Microsoft.ConnectedDevices;

/// <summary>
/// Handles messages that are sent across during an active session between two connected and authenticated devices. <br/>
/// Persists basic state (e.g. encryption) across sockets and transports (e.g. bt, wifi, ...).
/// </summary>
public sealed class CdpSession : IDisposable
{
    public ConnectedDevicesPlatform Platform { get; }
    public SessionId SessionId { get; private set; }

    public PeerCapabilities HostCapabilities { get; internal set; } = 0;
    public PeerCapabilities ClientCapabilities { get; internal set; } = 0;

    public CdpDeviceInfo? DeviceInfo => _connectHandler.DeviceInfo;

    readonly ILogger _logger;
    readonly ConnectHandler _connectHandler;
    readonly ChannelHandler _channelHandler;
    private CdpSession(ConnectedDevicesPlatform platform, EndpointInfo initialEndpoint, SessionId sessionId)
    {
        Platform = platform;
        SessionId = sessionId;

        _logger = platform.CreateLogger<CdpSession>();
        _connectHandler = ConnectHandler.Create(this, initialEndpoint);
        _channelHandler = ChannelHandler.Create(this);
    }

    #region Registration
    static readonly AutoKeyRegistry<uint, CdpSession> _sessionRegistry = [];
    internal static CdpSession GetOrCreate(ConnectedDevicesPlatform platform, EndpointInfo initialEndpoint, CommonHeader header)
    {
        ArgumentNullException.ThrowIfNull(initialEndpoint);
        ArgumentNullException.ThrowIfNull(header);

        var (_, localSessionId, remoteSessionId) = SessionId.Parse(header.SessionId);
        if (localSessionId != 0)
        {
            // Existing session
            var result = _sessionRegistry.Get(localSessionId);

            var expectedRemoteSessionId = result.SessionId.RemoteSessionId;
            if (expectedRemoteSessionId != 0 && expectedRemoteSessionId != remoteSessionId)
                throw new CdpSessionException($"Wrong {nameof(SessionId.RemoteSessionId)}");

            result.SessionId = result.SessionId.WithRemoteSessionId(remoteSessionId);

            // Do not check for device here!
            // See UpgradeHandler class

            result.ThrowIfDisposed();

            return result;
        }

        // Create
        return _sessionRegistry.Create(localSessionId => new(
            platform,
            initialEndpoint,
            sessionId: new(IsHost: true, localSessionId, remoteSessionId)
        ), out _);
    }

    internal static async Task<CdpSession> ConnectClientAsync(ConnectedDevicesPlatform platform, CdpSocket socket, ConnectOptions? options = null)
    {
        var session = _sessionRegistry.Create(localSessionId => new(
            platform,
            socket.Endpoint,
            sessionId: new(IsHost: false, localSessionId)
        ), out _);

        var connectHandler = (ClientConnectHandler)session._connectHandler;

        if (options is not null)
            connectHandler.UpgradeHandler.Upgraded += options.TransportUpgraded;

        await connectHandler.ConnectAsync(socket);

        return session;
    }
    #endregion

    #region SendMessage
    public void SendMessage(CdpSocket socket, CommonHeader header, EndianWriter payloadWriter, bool supplyRequestId = false)
        => SendMessage(socket, header, payloadWriter.Buffer.AsSpan(), supplyRequestId);

    uint _sequenceNumber = 0;
    ulong _requestId = 0;
    internal CdpCryptor? Cryptor { get; set; }
    public void SendMessage(CdpSocket socket, CommonHeader header, ReadOnlySpan<byte> payload, bool supplyRequestId = false)
    {
        if (header.Type == MessageType.Session && Cryptor == null)
            throw new InvalidOperationException("Invalid session state!");

        header.SessionId = SessionId.AsNumber();

        if (supplyRequestId)
            header.RequestID = Interlocked.Increment(ref _requestId);

        if (header.Type != MessageType.Connect)
            header.SequenceNumber = Interlocked.Increment(ref _sequenceNumber);

        // "CDPSvc" crashes if not supplied (AccessViolation in ShareHost.dll!ExtendCorrelationVector)
        if (header.Type == MessageType.Session)
            header.AdditionalHeaders.Add(AdditionalHeader.CreateCorrelationHeader());

        socket.SendMessage(header, payload, Cryptor);
    }
    #endregion

    #region HandleMessages
    bool _connectionEstablished = false;
    internal void HandleMessage(CdpSocket socket, CommonHeader header, ref EndianReader reader)
    {
        ThrowIfDisposed();

        Cryptor?.Read(ref reader, header);
        header.CorrectClientSessionBit();

        if (header.Type == MessageType.Connect)
        {
            if (_connectionEstablished)
                return;

            _connectHandler.HandleConnect(socket, header, ref reader);
            return;
        }

        if (!_connectHandler.UpgradeHandler.IsSocketAllowed(socket))
            throw UnexpectedMessage(socket.Endpoint.Address);

        if (Cryptor == null)
            throw UnexpectedMessage("Encryption");

        _connectionEstablished = true;

        switch (header.Type)
        {
            case MessageType.Control:
                _channelHandler.HandleControl(socket, header, ref reader);
                break;

            case MessageType.Session:
                HandleSession(header, ref reader);
                break;
        }
    }

    readonly ConcurrentDictionary<uint, CdpMessage> _msgRegistry = new();
    void HandleSession(CommonHeader header, ref EndianReader reader)
    {
        CdpMessage msg = _msgRegistry.GetOrAdd(header.SequenceNumber, id => new(header));
        msg.AddFragment(reader.ReadToEnd()); // ToDo: Reduce allocations

        if (msg.IsComplete)
        {
            try
            {
                var app = _channelHandler.GetChannelById(header.ChannelId).App ?? throw new InvalidOperationException($"No app for channel {header.ChannelId}");
                app.HandleMessage(msg);
            }
            finally
            {
                _msgRegistry.Remove(msg.Id, out _);
            }
        }
    }
    #endregion

    public async Task<CdpChannel> StartClientChannelAsync(string appId, string appName, CdpAppBase handler, CancellationToken cancellationToken = default)
    {
        if (_channelHandler is not ClientChannelHandler clientChannelHandler)
            throw new InvalidOperationException("Session is not a client");

        var socket = await Platform.CreateSocketAsync(_connectHandler.UpgradeHandler.RemoteEndpoint);
        return await clientChannelHandler.CreateChannelAsync(appId, appName, handler, socket, cancellationToken);
    }

    #region Utils
    public static Exception UnexpectedMessage(string? info = null)
        => new CdpSecurityException($"Received unexpected message {info ?? "null"}");
    #endregion

    #region Dispose
    public bool IsDisposed { get; private set; } = false;

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(IsDisposed, this);

    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;

        _sessionRegistry.Remove(SessionId.LocalSessionId);
        _msgRegistry.Clear();

        _channelHandler.Dispose();
    }
    #endregion
}
