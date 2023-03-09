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
    public required uint LocalSessionId { get; init; }
    public uint RemoteSessionId { get; private set; }
    public required ConnectedDevicesPlatform Platform { get; init; }
    public required bool IsHost { get; init; }

    public CdpDevice Device { get; }


    readonly UpgradeHandler _upgradeHandler;
    private CdpSession(CdpDevice device)
    {
        Device = device;

        _upgradeHandler = new(this, device);
    }

    internal ulong GetSessionId()
    {
        if (IsHost)
            return (ulong)LocalSessionId << 32 | RemoteSessionId | CommonHeader.SessionIdHostFlag;

        return (ulong)RemoteSessionId << 32 | LocalSessionId;
    }

    internal static void ParseSessionId(ulong sessionId, out uint localSessionId, out uint remoteSessionId, out bool isMsgFromHost)
    {
        isMsgFromHost = (sessionId & CommonHeader.SessionIdHostFlag) != 0;

        if (isMsgFromHost)
        {
            remoteSessionId = sessionId.HighValue();
            localSessionId = sessionId.LowValue() & ~(uint)CommonHeader.SessionIdHostFlag;
        }
        else
        {
            localSessionId = sessionId.HighValue();
            remoteSessionId = sessionId.LowValue();
        }
    }

    #region Registration
    static readonly AutoKeyRegistry<CdpSession> _sessionRegistry = new();
    internal static CdpSession GetOrCreate(ConnectedDevicesPlatform platform, CdpDevice device, CommonHeader header)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(header);

        var sessionId = header.SessionId;
        ParseSessionId(sessionId, out var localSessionId, out var remoteSessionId, out _);
        if (localSessionId != 0)
        {
            // Existing session
            var result = _sessionRegistry.Get(localSessionId);

            if (result.RemoteSessionId == 0)
                result.RemoteSessionId = remoteSessionId;

            if (result.RemoteSessionId != remoteSessionId)
                throw new CdpSessionException($"Wrong {nameof(RemoteSessionId)}");

            // Do not check for device here!
            // See UpgradeHandler class
            //if (result.Device.Address != device.Address)
            //    throw new CdpSessionException("Wrong device!");

            result.ThrowIfDisposed();

            return result;
        }
        else
        {
            // Create
            return _sessionRegistry.Create(localSessionId => new(device)
            {
                IsHost = true,
                Platform = platform,
                LocalSessionId = (uint)localSessionId,
                RemoteSessionId = remoteSessionId
            }, out _);
        }
    }

    internal static CdpSession CreateAndConnectClient(ConnectedDevicesPlatform platform, CdpSocket socket)
    {
        var session = _sessionRegistry.Create(localSessionId => new(socket.RemoteDevice)
        {
            IsHost = false,
            Platform = platform,
            LocalSessionId = (uint)localSessionId,
            RemoteSessionId = 0
        }, out _);

        session.SendConnectRequest(socket);

        return session;
    }

    void SendConnectRequest(CdpSocket socket)
    {
        CommonHeader header = new()
        {
            Type = MessageType.Connect,
            SessionId = GetSessionId()
        };

        SendMessage(socket, header, writer =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.ConnectRequest
            }.Write(writer);

            var publicKey = _localEncryption.PublicKey;
            new ConnectionRequest()
            {
                CurveType = CurveType.CT_NIST_P256_KDF_SHA512,
                HmacSize = Constants.HMacSize,
                MessageFragmentSize = Constants.DefaultMessageFragmentSize,
                Nonce = _localEncryption.Nonce,
                PublicKeyX = publicKey.X!,
                PublicKeyY = publicKey.Y!
            }.Write(writer);
        });
    }
    #endregion

    public void SendMessage(CdpSocket socket, CommonHeader header, BodyCallback bodyCallback, bool supplyRequestId = false)
    {
        if (header.Type == MessageType.Session && _cryptor == null)
            throw new InvalidOperationException("Invalid session state!");

        // header
        {
            header.SessionId = GetSessionId();

            if (supplyRequestId)
                header.RequestID = RequestId();

            if (header.Type != MessageType.Connect)
                header.SequenceNumber = SequenceNumber();

            // "CDPSvc" crashes if not supplied (AccessViolation in ShareHost.dll!ExtendCorrelationVector)
            if (header.Type == MessageType.Session)
            header.AdditionalHeaders.Add(AdditionalHeader.CreateCorrelationHeader());
        }

        EndianWriter writer = new(Endianness.BigEndian);
        if (_cryptor != null)
        {
            _cryptor.EncryptMessage(writer, header, bodyCallback);
        }
        else
        {
            EndianWriter payloadWriter = new(Endianness.BigEndian);
            bodyCallback(payloadWriter);
            var payload = payloadWriter.Buffer.AsSpan();

            header.SetPayloadLength(payload.Length);
            header.Write(writer);
            writer.Write(payload);
        }

        socket.SendData(writer);
    }

    #region HandleMessages
    CdpCryptor? _cryptor;
    readonly CdpEncryptionInfo _localEncryption = CdpEncryptionInfo.Create(CdpEncryptionParams.Default);
    CdpEncryptionInfo? _remoteEncryption = null;

    bool _connectionEstablished = false;
    public void HandleMessage(CdpSocket socket, CommonHeader header, EndianReader reader)
    {
        ThrowIfDisposed();

        EndianWriter writer = new(Endianness.BigEndian);
        var payloadReader = _cryptor != null ? _cryptor.Read(reader, header) : reader;

        header.CorrectClientSessionBit();

        // Only validate socket if it's not a connection msg
        // All upgrade messages (belong to connect) need to be seen
        // See HandleConnect(...)
        if (header.Type != MessageType.Connect && !_upgradeHandler.IsSocketAllowed(socket))
            throw UnexpectedMessage(socket.RemoteDevice.Address);

        if (header.Type == MessageType.Connect)
        {
            if (_connectionEstablished)
                throw UnexpectedMessage(header.Type.ToString());

            HandleConnect(socket, header, payloadReader);
        }
        else if (header.Type == MessageType.Control)
        {
            _connectionEstablished = true;

            HandleControl(header, payloadReader, socket);
        }
        else if (header.Type == MessageType.Session)
        {
            _connectionEstablished = true;

            HandleSession(header, payloadReader);
        }
        else
        {
            // We might receive a "ReliabilityResponse"
            // ignore
            // Platform.Handler.Log(0, $"Received {header.Type} message from session {header.SessionId.ToString("X")} via {socket.TransportType}");
        }
    }


    #region Connect
    void HandleConnect(CdpSocket socket, CommonHeader header, EndianReader reader)
    {
        ConnectionHeader connectionHeader = ConnectionHeader.Parse(reader);
        Platform.Handler.Log(0, $"Received {header.Type} message {connectionHeader.MessageType} from session {header.SessionId.ToString("X")} via {socket.TransportType}");

        if (_upgradeHandler.HandleConnect(socket, header, connectionHeader, reader))
            return;

        if (!_upgradeHandler.IsSocketAllowed(socket))
            throw UnexpectedMessage(socket.RemoteDevice.Address);

        if (connectionHeader.MessageType == ConnectionType.ConnectRequest)
        {
            if (_cryptor != null)
                throw UnexpectedMessage("Encryption");

            ThrowIfWrongMode(shouldBeHost: true);
            HandleConnectRequest(header, reader, socket);

            return;
        }

        if (connectionHeader.MessageType == ConnectionType.ConnectResponse)
        {
            if (_cryptor != null)
                throw UnexpectedMessage("Encryption");

            ThrowIfWrongMode(shouldBeHost: false);
            HandleConnectResponse(header, reader, socket);

            return;
        }

        if (_cryptor == null)
            throw UnexpectedMessage("Encryption");

        switch (connectionHeader.MessageType)
        {
            case ConnectionType.DeviceAuthRequest:
            case ConnectionType.UserDeviceAuthRequest:
                ThrowIfWrongMode(shouldBeHost: true);
                HandleAuthRequest(header, reader, socket, connectionHeader.MessageType);
                break;
            case ConnectionType.DeviceAuthResponse:
            case ConnectionType.UserDeviceAuthResponse:
                ThrowIfWrongMode(shouldBeHost: false);
                HandleAuthResponse(header, reader, socket, connectionHeader.MessageType);
                break;
            case ConnectionType.AuthDoneRequest:
                ThrowIfWrongMode(shouldBeHost: true);
                HandleAuthDoneRequest(header, socket);
                break;
            case ConnectionType.AuthDoneRespone:
                ThrowIfWrongMode(shouldBeHost: false);
                HandleAuthDoneResponse(socket, reader);

                socket.Dispose();
                break;
            case ConnectionType.DeviceInfoMessage:
                HandleDeviceInfoMessage(header, reader, socket);
                break;
            default:
                throw UnexpectedMessage(connectionHeader.MessageType.ToString());
        }
    }

    void HandleConnectRequest(CommonHeader header, EndianReader reader, CdpSocket socket)
    {
        var connectionRequest = ConnectionRequest.Parse(reader);
        _remoteEncryption = CdpEncryptionInfo.FromRemote(connectionRequest.PublicKeyX, connectionRequest.PublicKeyY, connectionRequest.Nonce, CdpEncryptionParams.Default);

        SendMessage(socket, header, writer =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.ConnectResponse
            }.Write(writer);

            var publicKey = _localEncryption.PublicKey;
            new ConnectionResponse()
            {
                Result = ConnectionResult.Pending,
                HmacSize = connectionRequest.HmacSize,
                MessageFragmentSize = connectionRequest.MessageFragmentSize,
                Nonce = _localEncryption.Nonce,
                PublicKeyX = publicKey.X!,
                PublicKeyY = publicKey.Y!
            }.Write(writer);
        });

        // We have to set cryptor after we send the message because it would be encrypted otherwise
        var secret = _localEncryption.GenerateSharedSecret(_remoteEncryption);
        _cryptor = new(secret);
    }
    void HandleConnectResponse(CommonHeader header, EndianReader reader, CdpSocket socket)
    {
        var connectionResponse = ConnectionResponse.Parse(reader);
        _remoteEncryption = CdpEncryptionInfo.FromRemote(connectionResponse.PublicKeyX, connectionResponse.PublicKeyY, connectionResponse.Nonce, CdpEncryptionParams.Default);

        var secret = _localEncryption.GenerateSharedSecret(_remoteEncryption);
        _cryptor = new(secret);

        header.Flags = 0;
        SendMessage(socket, header, (writer) =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.DeviceAuthRequest
            }.Write(writer);
            AuthenticationPayload.Create(
                _localEncryption.DeviceCertificate!, // ToDo: User cert
                hostNonce: _remoteEncryption!.Nonce, clientNonce: _localEncryption.Nonce
            ).Write(writer);
        });
    }
    void HandleAuthRequest(CommonHeader header, EndianReader reader, CdpSocket socket, ConnectionType connectionType)
    {
        var authRequest = AuthenticationPayload.Parse(reader);
        if (!authRequest.VerifyThumbprint(hostNonce: _localEncryption.Nonce, clientNonce: _remoteEncryption!.Nonce))
            throw new CdpSecurityException("Invalid thumbprint");

        header.Flags = 0;
        SendMessage(socket, header, (writer) =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = connectionType == ConnectionType.DeviceAuthRequest ? ConnectionType.DeviceAuthResponse : ConnectionType.UserDeviceAuthResponse
            }.Write(writer);
            AuthenticationPayload.Create(
                _localEncryption.DeviceCertificate!, // ToDo: User cert
                hostNonce: _localEncryption.Nonce, clientNonce: _remoteEncryption!.Nonce
            ).Write(writer);
        });
    }
    void HandleAuthResponse(CommonHeader header, EndianReader reader, CdpSocket socket, ConnectionType connectionType)
    {
        var authRequest = AuthenticationPayload.Parse(reader);
        if (!authRequest.VerifyThumbprint(hostNonce: _remoteEncryption!.Nonce, clientNonce: _localEncryption.Nonce))
            throw new CdpSecurityException("Invalid thumbprint");

        if (connectionType == ConnectionType.DeviceAuthResponse)
        {
            header.Flags = 0;
            SendMessage(socket, header, (writer) =>
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.UserDeviceAuthRequest
                }.Write(writer);
                AuthenticationPayload.Create(
                    _localEncryption.DeviceCertificate!, // ToDo: User cert
                    hostNonce: _remoteEncryption!.Nonce, clientNonce: _localEncryption.Nonce
                ).Write(writer);
            });

            return;
        }

        header.Flags = 0;
        SendMessage(socket, header, (writer) =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.AuthDoneRequest
            }.Write(writer);
        });
    }
    void HandleAuthDoneRequest(CommonHeader header, CdpSocket socket)
    {
        header.Flags = 0;
        SendMessage(socket, header, (writer) =>
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

    #region OnAuthDone
    event Action? OnAuthDoneInternal;
    public Task WaitForAuthDone()
    {
        TaskCompletionSource promise = new();
        void callback()
        {
            promise.SetResult();
            OnAuthDoneInternal -= callback;
        }
        OnAuthDoneInternal += callback;
        return promise.Task;
    }
    #endregion

    void HandleAuthDoneResponse(CdpSocket socket, EndianReader reader)
    {
        var msg = ResultPayload.Parse(reader);
        msg.ThrowOnError();
        OnAuthDoneInternal?.Invoke();
        socket.Dispose();
    }
    void HandleDeviceInfoMessage(CommonHeader header, EndianReader reader, CdpSocket socket)
    {
        var msg = DeviceInfoMessage.Parse(reader);

        header.Flags = 0;
        SendMessage(socket, header, (writer) =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.DeviceInfoResponseMessage // Ack
            }.Write(writer);
        });
    }
    #endregion

    #region Control
    void HandleControl(CommonHeader header, EndianReader reader, CdpSocket socket)
    {
        if (_cryptor == null)
            throw UnexpectedMessage("Encryption");

        var controlHeader = ControlHeader.Parse(reader);
        Platform.Handler.Log(0, $"Received {header.Type} message {controlHeader.MessageType} from session {header.SessionId.ToString("X")} via {socket.TransportType}");
        switch (controlHeader.MessageType)
        {
            case ControlMessageType.StartChannelRequest:
                HandleStartChannelRequest(header, reader, socket);
                break;
            case ControlMessageType.StartChannelResponse:
                HandleStartChannelResponse(header, reader, socket);
                break;
            default:
                throw UnexpectedMessage(controlHeader.MessageType.ToString());
        }
    }

    void HandleStartChannelRequest(CommonHeader header, EndianReader reader, CdpSocket socket)
    {
        var request = StartChannelRequest.Parse(reader);

        header.AdditionalHeaders.Clear();
        header.SetReplyToId(header.RequestID);
        header.AdditionalHeaders.Add(new(
            (AdditionalHeaderType)129,
            new byte[] { 0x30, 0x0, 0x0, 0x1 }
        ));
        header.RequestID = 0;

        InitializeHostChannel(request, socket, out var channelId);

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
    Task<StartChannelResponse> WaitForChannelResponse(ulong requestId)
    {
        TaskCompletionSource<StartChannelResponse> promise = new();
        void callback(CommonHeader header, StartChannelResponse response)
        {
            if (header.TryGetReplyToId() == requestId)
                promise.SetResult(response);

            OnStartChannelResponseInternal -= callback;
        }
        OnStartChannelResponseInternal += callback;
        return promise.Task;
    }

    void HandleStartChannelResponse(CommonHeader header, EndianReader reader, CdpSocket socket)
    {
        var msg = StartChannelResponse.Parse(reader);
        OnStartChannelResponseInternal?.Invoke(header, msg);
    }
    #endregion

    void HandleSession(CommonHeader header, EndianReader reader)
    {
        if (_cryptor == null)
            throw UnexpectedMessage("Encryption");

        CdpMessage msg = GetOrCreateMessage(header);
        msg.AddFragment(reader.ReadToEnd());

        if (msg.IsComplete)
        {
            try
            {
                _channelRegistry.Get(header.ChannelId)
                    .HandleMessageAsync(msg);
            }
            finally
            {
                _msgRegistry.Remove(msg.Id, out _);
                msg.Dispose();
            }
        }
    }
    #endregion

    #region SequenceNumber
    uint _sequenceNumber = 0;
    internal uint SequenceNumber()
        => Interlocked.Increment(ref _sequenceNumber);

    ulong _requestId = 0;
    internal ulong RequestId()
        => Interlocked.Increment(ref _requestId);
    #endregion

    #region Messages
    readonly ConcurrentDictionary<uint, CdpMessage> _msgRegistry = new();
    CdpMessage GetOrCreateMessage(CommonHeader header)
        => _msgRegistry.GetOrAdd(header.SequenceNumber, (id) => new(header));
    #endregion

    #region Channels
    readonly AutoKeyRegistry<CdpChannel> _channelRegistry = new();

    void InitializeHostChannel(StartChannelRequest request, CdpSocket socket, out ulong channelId)
    {
        if (!IsHost)
            throw new InvalidOperationException("Session is not a host");

        _channelRegistry.Create(channelId =>
        {
            var app = CdpAppRegistration.InstantiateApp(request.Id, request.Name);
            CdpChannel channel = new(this, channelId, app, socket);
            app.Channel = channel;
            return channel;
        }, out channelId);
    }

    public async Task<CdpChannel> StartClientChannelAsync(string appId, string appName, CdpAppBase handler)
    {
        if (IsHost)
            throw new InvalidOperationException("Session is not a client");

        var socket = await Platform.CreateSocketAsync(Device);
        return await StartClientChannelAsync(appId, appName, handler, socket);
    }

    public async Task<CdpChannel> StartClientChannelAsync(string appId, string appName, CdpAppBase handler, CdpSocket socket)
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

        var response = await WaitForChannelResponse(header.RequestID);
        response.ThrowOnError();

        CdpChannel channel = new(this, response.ChannelId, handler, socket);
        handler.Channel = channel;
        _channelRegistry.Add(channel.ChannelId, channel);
        return channel;
    }

    internal void UnregisterChannel(CdpChannel channel)
        => _channelRegistry.Remove(channel.ChannelId);
    #endregion

    public static Exception UnexpectedMessage(string? info = null)
        => new CdpSecurityException($"Received unexpected message {info ?? "null"}");

    void ThrowIfWrongMode(bool shouldBeHost)
    {
        if (shouldBeHost && !IsHost || !shouldBeHost && IsHost)
            throw UnexpectedMessage($"{(shouldBeHost ? "client" : "host")} msg");
    }

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

        _sessionRegistry.Remove(LocalSessionId);

        foreach (var channel in _channelRegistry)
            channel.Dispose();
        _channelRegistry.Clear();

        foreach (var msg in _msgRegistry.Values)
            msg.Dispose();
        _msgRegistry.Clear();
    }
}
