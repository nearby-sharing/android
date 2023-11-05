using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Internal;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;
using ShortDev.Microsoft.ConnectedDevices.Messages.Control;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices;

/// <summary>
/// Handles messages that are sent across during an active session between two connected and authenticated devices. <br/>
/// Persists basic state (e.g. encryption) across sockets and transports (e.g. bt, wifi, ...).
/// </summary>
public sealed class CdpSession : IDisposable
{
    public SessionId SessionId { get; private set; }
    public uint RemoteSessionId { get; private set; }

    public bool IsHost => SessionId.IsHost;
    public PeerCapabilities HostCapabilities { get; private set; } = 0;
    public PeerCapabilities ClientCapabilities { get; private set; } = 0;

    public ConnectedDevicesPlatform Platform { get; }
    public CdpDevice Device { get; private set; }

    readonly ILogger<CdpSession> _logger;
    readonly UpgradeHandler _upgradeHandler;
    readonly ConnectHandler _connectHandler;
    private CdpSession(ConnectedDevicesPlatform platform, CdpDevice device, SessionId sessionId)
    {
        Platform = platform;
        Device = device;
        SessionId = sessionId;

        _logger = platform.CreateLogger<CdpSession>();
        _upgradeHandler = new(this, device);
        _connectHandler = new(this, _upgradeHandler);
    }

    #region Registration
    static readonly AutoKeyRegistry<uint, CdpSession> _sessionRegistry = new();
    internal static CdpSession GetOrCreate(ConnectedDevicesPlatform platform, CdpDevice device, CommonHeader header)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(header);

        var (_, localSessionId, remoteSessionId) = SessionId.Parse(header.SessionId);
        if (localSessionId != 0)
        {
            // Existing session
            var result = _sessionRegistry.Get(localSessionId);

            var expectedRemoteSessionId = result.SessionId.RemoteSessionId;
            if (expectedRemoteSessionId != 0 && expectedRemoteSessionId != remoteSessionId)
                throw new CdpSessionException($"Wrong {nameof(RemoteSessionId)}");

            result.SessionId = result.SessionId.WithRemoteSessionId(remoteSessionId);

            // Do not check for device here!
            // See UpgradeHandler class
            //if (result.Device.Address != device.Address)
            //    throw new CdpSessionException("Wrong device!");

            result.ThrowIfDisposed();

            return result;
        }

        // Create
        return _sessionRegistry.Create(localSessionId => new(
            platform,
            device,
            sessionId: new(IsHost: true, localSessionId, remoteSessionId)
        ), out _);
    }

    internal static async Task<CdpSession> CreateClientAndConnectAsync(ConnectedDevicesPlatform platform, CdpSocket socket)
    {
        var session = _sessionRegistry.Create(localSessionId => new(
            platform,
            socket.RemoteDevice,
            sessionId: new(IsHost: false, localSessionId)
        ), out _);

        await session._connectHandler.ConnectAsync(socket);

        return session;
    }
    #endregion

    public void SendMessage(CdpSocket socket, CommonHeader header, BodyCallback bodyCallback, bool supplyRequestId = false)
    {
        if (header.Type == MessageType.Session && _cryptor == null)
            throw new InvalidOperationException("Invalid session state!");

        // header
        {
            header.SessionId = SessionId.AsNumber();

            if (supplyRequestId)
                header.RequestID = RequestId();

            if (header.Type != MessageType.Connect)
                header.SequenceNumber = SequenceNumber();

            // "CDPSvc" crashes if not supplied (AccessViolation in ShareHost.dll!ExtendCorrelationVector)
            if (header.Type == MessageType.Session)
                header.AdditionalHeaders.Add(AdditionalHeader.CreateCorrelationHeader());
        }

        EndianWriter payloadWriter = new(Endianness.BigEndian);
        bodyCallback(payloadWriter);
        var payload = payloadWriter.Buffer.AsSpan();

        if (payload.Length <= Constants.DefaultMessageFragmentSize)
        {
            SendFragment(header, payload);
            return;
        }

        header.FragmentCount = (ushort)(payload.Length / Constants.DefaultMessageFragmentSize);

        var leftover = payload.Length % Constants.DefaultMessageFragmentSize;
        if (leftover != 0)
            header.FragmentCount++;

        for (ushort fragmentIndex = 0; fragmentIndex < header.FragmentCount; fragmentIndex++)
        {
            header.FragmentIndex = fragmentIndex;

            int start = fragmentIndex * Constants.DefaultMessageFragmentSize;
            int length = Math.Min(payload.Length - start, Constants.DefaultMessageFragmentSize);
            SendFragment(header, payload.Slice(start, length));
        }

        void SendFragment(CommonHeader header, ReadOnlySpan<byte> fragmentPayload)
        {
            EndianWriter writer = new(Endianness.BigEndian);
            if (_cryptor != null)
            {
                _cryptor.EncryptMessage(writer, header, fragmentPayload);
            }
            else
            {
                header.SetPayloadLength(fragmentPayload.Length);
                header.Write(writer);
                writer.Write(fragmentPayload);
            }

            socket.SendData(writer);
        }
    }

    #region HandleMessages
    bool _connectionEstablished = false;
    public void HandleMessage(CdpSocket socket, CommonHeader header, ref EndianReader reader)
    {
        ThrowIfDisposed();

        _cryptor?.Read(ref reader, header);

        header.CorrectClientSessionBit();

        if (header.Type == MessageType.Connect)
        {
            if (_connectionEstablished)
                return;

            _connectHandler.HandleConnect(socket, header, ref reader);
        }

        if (!_upgradeHandler.IsSocketAllowed(socket))
            throw UnexpectedMessage(socket.RemoteDevice.Endpoint.Address);

        if (header.Type == MessageType.Control)
        {
            _connectionEstablished = true;

            HandleControl(header, ref reader, socket);
        }
        else if (header.Type == MessageType.Session)
        {
            _connectionEstablished = true;

            HandleSession(header, ref reader);
        }
    }

    CdpCryptor? _cryptor;
    readonly CdpEncryptionInfo _localEncryption = CdpEncryptionInfo.Create(CdpEncryptionParams.Default);
    CdpEncryptionInfo? _remoteEncryption = null;
    sealed class ConnectHandler(CdpSession session, UpgradeHandler upgradeHandler)
    {
        readonly ILogger<ConnectHandler> _logger = session.Platform.CreateLogger<ConnectHandler>();
        readonly CdpSession _session = session;
        readonly UpgradeHandler _upgradeHandler = upgradeHandler;

        TaskCompletionSource? _currentConnectPromise;
        public async Task ConnectAsync(CdpSocket socket, bool upgradeSupported = true)
        {
            if (_currentConnectPromise != null)
                throw new InvalidOperationException("Already connecting");

            _currentConnectPromise = new();

            CommonHeader header = new()
            {
                Type = MessageType.Connect,
                AdditionalHeaders =
                {
                    AdditionalHeader.FromUInt32(AdditionalHeaderType.Header129, 0x70_00_00_03),
                    AdditionalHeader.FromUInt64(AdditionalHeaderType.PeerCapabilities, (ulong)PeerCapabilities.All),
                    AdditionalHeader.FromUInt64(AdditionalHeaderType.Header131, upgradeSupported ? 7u : 6u)
                }
            };

            _session.SendMessage(socket, header, writer =>
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.ConnectRequest
                }.Write(writer);

                var publicKey = _session._localEncryption.PublicKey;
                new ConnectionRequest()
                {
                    CurveType = CurveType.CT_NIST_P256_KDF_SHA512,
                    HmacSize = Constants.HMacSize,
                    MessageFragmentSize = Constants.DefaultMessageFragmentSize,
                    Nonce = _session._localEncryption.Nonce,
                    PublicKeyX = publicKey.X!,
                    PublicKeyY = publicKey.Y!
                }.Write(writer);
            });

            await _currentConnectPromise.Task;
        }

        public void HandleConnect(CdpSocket socket, CommonHeader header, ref EndianReader reader)
        {
            ConnectionHeader connectionHeader = ConnectionHeader.Parse(ref reader);
            _logger.ReceivedConnectMessage(
                connectionHeader.MessageType,
                header.SessionId,
                socket.TransportType
            );

            if (_upgradeHandler.TryHandleConnect(socket, connectionHeader, ref reader))
                return;

            if (!_upgradeHandler.IsSocketAllowed(socket))
                throw UnexpectedMessage(socket.RemoteDevice.Endpoint.Address);

            if (connectionHeader.MessageType == ConnectionType.ConnectRequest)
            {
                if (_session._cryptor != null)
                    throw UnexpectedMessage("Encryption");

                _session.ThrowIfWrongMode(shouldBeHost: true);
                HandleConnectRequest(header, ref reader, socket);
                return;
            }

            if (connectionHeader.MessageType == ConnectionType.ConnectResponse)
            {
                if (_session._cryptor != null)
                    throw UnexpectedMessage("Encryption");

                _session.ThrowIfWrongMode(shouldBeHost: false);
                HandleConnectResponse(header, ref reader, socket);
                return;
            }

            if (_session._cryptor == null)
                throw UnexpectedMessage("Encryption");

            switch (connectionHeader.MessageType)
            {
                case ConnectionType.DeviceAuthRequest:
                case ConnectionType.UserDeviceAuthRequest:
                    _session.ThrowIfWrongMode(shouldBeHost: true);
                    HandleAuthRequest(header, ref reader, socket, connectionHeader.MessageType);
                    break;

                case ConnectionType.DeviceAuthResponse:
                case ConnectionType.UserDeviceAuthResponse:
                    _session.ThrowIfWrongMode(shouldBeHost: false);
                    HandleAuthResponse(header, ref reader, socket, connectionHeader.MessageType);
                    break;

                case ConnectionType.AuthDoneRequest:
                    _session.ThrowIfWrongMode(shouldBeHost: true);
                    HandleAuthDoneRequest(header, socket);
                    break;

                case ConnectionType.AuthDoneRespone:
                    _session.ThrowIfWrongMode(shouldBeHost: false);
                    HandleAuthDoneResponse(socket, ref reader);
                    break;

                case ConnectionType.DeviceInfoMessage:
                    HandleDeviceInfoMessage(header, ref reader, socket);
                    break;

                case ConnectionType.DeviceInfoResponseMessage:
                    break;
            }
        }

        void HandleConnectRequest(CommonHeader header, ref EndianReader reader, CdpSocket socket)
        {
            _session.ClientCapabilities = (PeerCapabilities)(header.TryGetHeader(AdditionalHeaderType.PeerCapabilities)?.AsUInt64() ?? 0);

            var connectionRequest = ConnectionRequest.Parse(ref reader);
            _session._remoteEncryption = CdpEncryptionInfo.FromRemote(connectionRequest.PublicKeyX, connectionRequest.PublicKeyY, connectionRequest.Nonce, CdpEncryptionParams.Default);

            _session.SendMessage(socket, header, writer =>
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.ConnectResponse
                }.Write(writer);

                var publicKey = _session._localEncryption.PublicKey;
                new ConnectionResponse()
                {
                    Result = ConnectionResult.Pending,
                    HmacSize = connectionRequest.HmacSize,
                    MessageFragmentSize = connectionRequest.MessageFragmentSize,
                    Nonce = _session._localEncryption.Nonce,
                    PublicKeyX = publicKey.X!,
                    PublicKeyY = publicKey.Y!
                }.Write(writer);
            });

            // We have to set cryptor after we send the message because it would be encrypted otherwise
            var secret = _session._localEncryption.GenerateSharedSecret(_session._remoteEncryption);
            _session._cryptor = new(secret);
        }

        void HandleConnectResponse(CommonHeader header, ref EndianReader reader, CdpSocket socket)
        {
            _session.HostCapabilities = (PeerCapabilities)(header.TryGetHeader(AdditionalHeaderType.PeerCapabilities)?.AsUInt64() ?? 0);

            var connectionResponse = ConnectionResponse.Parse(ref reader);
            _session._remoteEncryption = CdpEncryptionInfo.FromRemote(connectionResponse.PublicKeyX, connectionResponse.PublicKeyY, connectionResponse.Nonce, CdpEncryptionParams.Default);

            var secret = _session._localEncryption.GenerateSharedSecret(_session._remoteEncryption);
            _session._cryptor = new(secret);

            header.Flags = 0;
            _session.SendMessage(socket, header, (writer) =>
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.DeviceAuthRequest
                }.Write(writer);
                AuthenticationPayload.Create(
                    _session.Platform.DeviceInfo.DeviceCertificate!, // ToDo: User cert
                    hostNonce: _session._remoteEncryption!.Nonce, clientNonce: _session._localEncryption.Nonce
                ).Write(writer);
            });
        }

        void HandleAuthRequest(CommonHeader header, ref EndianReader reader, CdpSocket socket, ConnectionType connectionType)
        {
            var authRequest = AuthenticationPayload.Parse(ref reader);
            if (!authRequest.VerifyThumbprint(hostNonce: _session._localEncryption.Nonce, clientNonce: _session._remoteEncryption!.Nonce))
                throw new CdpSecurityException("Invalid thumbprint");

            header.Flags = 0;
            _session.SendMessage(socket, header, (writer) =>
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = connectionType == ConnectionType.DeviceAuthRequest ? ConnectionType.DeviceAuthResponse : ConnectionType.UserDeviceAuthResponse
                }.Write(writer);
                AuthenticationPayload.Create(
                    _session.Platform.DeviceInfo.DeviceCertificate, // ToDo: User cert
                    hostNonce: _session._localEncryption.Nonce, clientNonce: _session._remoteEncryption!.Nonce
                ).Write(writer);
            });
        }

        void HandleAuthResponse(CommonHeader header, ref EndianReader reader, CdpSocket socket, ConnectionType connectionType)
        {
            var authRequest = AuthenticationPayload.Parse(ref reader);
            if (!authRequest.VerifyThumbprint(hostNonce: _session._remoteEncryption!.Nonce, clientNonce: _session._localEncryption.Nonce))
                throw new CdpSecurityException("Invalid thumbprint");

            if (connectionType == ConnectionType.DeviceAuthResponse)
            {
                header.Flags = 0;
                _session.SendMessage(socket, header, (writer) =>
                {
                    new ConnectionHeader()
                    {
                        ConnectionMode = ConnectionMode.Proximal,
                        MessageType = ConnectionType.UserDeviceAuthRequest
                    }.Write(writer);
                    AuthenticationPayload.Create(
                        _session.Platform.DeviceInfo.DeviceCertificate!, // ToDo: User cert
                        hostNonce: _session._remoteEncryption!.Nonce, clientNonce: _session._localEncryption.Nonce
                    ).Write(writer);
                });

                return;
            }

            PrepareSession(socket);

            async void PrepareSession(CdpSocket socket)
            {
                if (socket.TransportType == Transports.CdpTransportType.Rfcomm)
                {
                    try
                    {
                        var oldSocket = socket;
                        socket = await _upgradeHandler.RequestUpgradeAsync(oldSocket);
                        oldSocket.Dispose();

                        _session.Device = socket.RemoteDevice;
                    }
                    catch (Exception ex)
                    {
                        _logger.UpgradeFailed(ex);
                    }
                }

                header.Flags = 0;
                _session.SendMessage(socket, header, (writer) =>
                {
                    new ConnectionHeader()
                    {
                        ConnectionMode = ConnectionMode.Proximal,
                        MessageType = ConnectionType.AuthDoneRequest
                    }.Write(writer);
                });
            }
        }

        void HandleAuthDoneRequest(CommonHeader header, CdpSocket socket)
        {
            header.Flags = 0;
            _session.SendMessage(socket, header, (writer) =>
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.AuthDoneRespone // Ack
                }.Write(writer);
                new ResultPayload()
                {
                    Result = CdpResult.Success
                }.Write(writer);
            });
        }

        void HandleAuthDoneResponse(CdpSocket socket, ref EndianReader reader)
        {
            var msg = ResultPayload.Parse(ref reader);
            msg.ThrowOnError();

            _session.SendMessage(socket, new CommonHeader()
            {
                Type = MessageType.Connect
            },
            (writer) =>
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.DeviceInfoMessage
                }.Write(writer);
                new DeviceInfoMessage()
                {
                    DeviceInfo = _session.Platform.GetCdpDeviceInfo()
                }.Write(writer);
            });

            _currentConnectPromise?.TrySetResult();
        }

        void HandleDeviceInfoMessage(CommonHeader header, ref EndianReader reader, CdpSocket socket)
        {
            var msg = DeviceInfoMessage.Parse(ref reader);
            _logger.ReceivedDeviceInfo(msg.DeviceInfo);

            header.Flags = 0;
            _session.SendMessage(socket, header, (writer) =>
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.DeviceInfoResponseMessage // Ack
                }.Write(writer);
            });
        }
    }

    #region Control
    void HandleControl(CommonHeader header, ref EndianReader reader, CdpSocket socket)
    {
        if (_cryptor == null)
            throw UnexpectedMessage("Encryption");

        var controlHeader = ControlHeader.Parse(ref reader);
        _logger.ReceivedControlMessage(
            controlHeader.MessageType,
            header.SessionId,
            socket.TransportType
        );
        switch (controlHeader.MessageType)
        {
            case ControlMessageType.StartChannelRequest:
                HandleStartChannelRequest(header, ref reader, socket);
                break;
            case ControlMessageType.StartChannelResponse:
                HandleStartChannelResponse(header, ref reader);
                break;
            default:
                throw UnexpectedMessage(controlHeader.MessageType.ToString());
        }
    }

    void HandleStartChannelRequest(CommonHeader header, ref EndianReader reader, CdpSocket socket)
    {
        var request = StartChannelRequest.Parse(ref reader);

        header.AdditionalHeaders.Clear();
        header.SetReplyToId(header.RequestID);
        header.AdditionalHeaders.Add(new(
            (AdditionalHeaderType)129,
            new byte[] { 0x30, 0x0, 0x0, 0x1 }
        ));
        header.RequestID = 0;

        _channelRegistry.Create(channelId => CdpChannel.CreateServerChannel(this, channelId, socket, request), out var channelId);

        header.Flags = 0;
        SendMessage(socket, header, (writer) =>
        {
            new ControlHeader()
            {
                MessageType = ControlMessageType.StartChannelResponse
            }.Write(writer);
            new StartChannelResponse()
            {
                Result = ChannelResult.Success,
                ChannelId = channelId
            }.Write(writer);
        });
    }

    event Action<CommonHeader, StartChannelResponse>? OnStartChannelResponseInternal;
    Task<StartChannelResponse> WaitForChannelResponse(ulong requestId, CancellationToken cancellationToken)
    {
        TaskCompletionSource<StartChannelResponse> promise = new();
        void callback(CommonHeader header, StartChannelResponse response)
        {
            if (header.TryGetReplyToId() == requestId)
                promise.SetResult(response);

            OnStartChannelResponseInternal -= callback;
        }
        OnStartChannelResponseInternal += callback;

        cancellationToken.Register(() =>
        {
            OnStartChannelResponseInternal -= callback;
            promise.TrySetCanceled();
        });

        return promise.Task;
    }

    void HandleStartChannelResponse(CommonHeader header, ref EndianReader reader)
    {
        var msg = StartChannelResponse.Parse(ref reader);
        OnStartChannelResponseInternal?.Invoke(header, msg);
    }
    #endregion

    void HandleSession(CommonHeader header, ref EndianReader reader)
    {
        if (_cryptor == null)
            throw UnexpectedMessage("Encryption");

        CdpMessage msg = GetOrCreateMessage(header);
        msg.AddFragment(reader.ReadToEnd()); // ToDo: Reduce allocations

        if (msg.IsComplete)
        {
            try
            {
                var app = _channelRegistry.Get(header.ChannelId).App ?? throw new InvalidOperationException($"No app for channel {header.ChannelId}");
                app.HandleMessage(msg);
            }
            finally
            {
                _msgRegistry.Remove(msg.Id, out _);
            }
        }
    }
    #endregion

    #region IDs
    uint _sequenceNumber = 0;
    uint SequenceNumber()
        => Interlocked.Increment(ref _sequenceNumber);

    ulong _requestId = 0;
    ulong RequestId()
        => Interlocked.Increment(ref _requestId);
    #endregion

    #region Message Registration
    readonly ConcurrentDictionary<uint, CdpMessage> _msgRegistry = new();
    CdpMessage GetOrCreateMessage(CommonHeader header)
        => _msgRegistry.GetOrAdd(header.SequenceNumber, (id) => new(header));
    #endregion

    #region Channels
    readonly AutoKeyRegistry<ulong, CdpChannel> _channelRegistry = new();

    public async Task<CdpChannel> StartClientChannelAsync(string appId, string appName, CdpAppBase handler, CancellationToken cancellationToken = default)
    {
        if (IsHost)
            throw new InvalidOperationException("Session is not a client");

        var socket = await Platform.CreateSocketAsync(Device);
        return await StartClientChannelAsync(appId, appName, handler, socket, cancellationToken);
    }

    public async Task<CdpChannel> StartClientChannelAsync(string appId, string appName, CdpAppBase handler, CdpSocket socket, CancellationToken cancellationToken = default)
    {
        if (IsHost)
            throw new InvalidOperationException("Session is not a client");

        CommonHeader header = new()
        {
            Type = MessageType.Control
        };
        SendMessage(
            socket, header,
            writer =>
            {
                new ControlHeader()
                {
                    MessageType = ControlMessageType.StartChannelRequest
                }.Write(writer);
                new StartChannelRequest()
                {
                    Id = appId,
                    Name = appName
                }.Write(writer);
            },
            supplyRequestId: true
        );

        var response = await WaitForChannelResponse(header.RequestID, cancellationToken);
        response.ThrowOnError();

        var channel = CdpChannel.CreateClientChannel(this, socket, response, handler);
        _channelRegistry.Add(channel.ChannelId, channel);

        return channel;
    }

    internal void UnregisterChannel(CdpChannel channel)
        => _channelRegistry.Remove(channel.ChannelId);
    #endregion

    #region Utils
    public static Exception UnexpectedMessage(string? info = null)
        => new CdpSecurityException($"Received unexpected message {info ?? "null"}");

    internal void ThrowIfWrongMode(bool shouldBeHost)
    {
        if (shouldBeHost && !IsHost || !shouldBeHost && IsHost)
            throw UnexpectedMessage($"{(shouldBeHost ? "client" : "host")} msg");
    }
    #endregion

    #region Dispose
    public bool IsDisposed { get; private set; } = false;

    private void ThrowIfDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(CdpSession));
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;

        _sessionRegistry.Remove(SessionId.LocalSessionId);

        foreach (var channel in _channelRegistry)
            channel.Dispose();
        _channelRegistry.Clear();

        _msgRegistry.Clear();
    }
    #endregion
}
