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
using System.Formats.Asn1;
using System.IO;
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
                MessageType = ConnectionType.ConnectResponse
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

    public void SendMessage(CdpSocket socket, CommonHeader header, Action<BinaryWriter> bodyCallback)
        => SendMessage(socket.Writer, header, bodyCallback);

    void SendMessage(BinaryWriter writer, CommonHeader header, Action<BinaryWriter> bodyCallback)
    {
        if (Cryptor != null)
        {
            Cryptor.EncryptMessage(writer, header, bodyCallback);
            return;
        }

        byte[] payload;
        using (MemoryStream payloadStream = new())
        using (BigEndianBinaryWriter payloadWriter = new(payloadStream))
        {
            bodyCallback(payloadWriter);
            payload = payloadStream.ToArray();
        }

        header.SetPayloadLength(payload.Length);
        header.Write(writer);
        writer.Write(payload);
    }

    #region HandleMessages
    internal CdpCryptor? Cryptor { get; private set; } = null;
    readonly CdpEncryptionInfo _localEncryption = CdpEncryptionInfo.Create(CdpEncryptionParams.Default);
    CdpEncryptionInfo? _remoteEncryption = null;

    bool _connectionEstablished = false;
    public void HandleMessage(CdpSocket socket, CommonHeader header, BinaryReader reader)
    {
        ThrowIfDisposed();

        var writer = socket.Writer;
        BinaryReader payloadReader = Cryptor?.Read(reader, header) ?? reader;
        {
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

                HandleConnect(socket, header, payloadReader, writer);
            }
            else if (header.Type == MessageType.Control)
            {
                _connectionEstablished = true;

                HandleControl(header, payloadReader, writer, socket);
            }
            else if (header.Type == MessageType.Session)
            {
                _connectionEstablished = true;

                HandleSession(header, payloadReader, writer);
            }
            else
            {
                // We might receive a "ReliabilityResponse"
                // ignore
                Platform.Handler.Log(0, $"Received {header.Type} message from session {header.SessionId.ToString("X")} via {socket.TransportType}");
            }
        }

        writer.Flush();
    }

    #region Connect
    void HandleConnect(CdpSocket socket, CommonHeader header, BinaryReader reader, BinaryWriter writer)
    {
        ConnectionHeader connectionHeader = ConnectionHeader.Parse(reader);
        Platform.Handler.Log(0, $"Received {header.Type} message {connectionHeader.MessageType} from session {header.SessionId.ToString("X")} via {socket.TransportType}");

        if (_upgradeHandler.HandleConnect(socket, header, connectionHeader, reader, writer))
            return;

        if (!_upgradeHandler.IsSocketAllowed(socket))
            throw UnexpectedMessage(socket.RemoteDevice.Address);

        if (connectionHeader.MessageType == ConnectionType.ConnectRequest)
        {
            if (Cryptor != null)
                throw UnexpectedMessage("Encryption");

            ThrowIfWrongMode(shouldBeHost: true);
            HandleConnectRequest(header, reader, writer);

            return;
        }

        if (Cryptor == null)
            throw UnexpectedMessage("Encryption");

        switch (connectionHeader.MessageType)
        {
            case ConnectionType.ConnectResponse:
                ThrowIfWrongMode(shouldBeHost: false);
                HandleConnectResponse(header, reader, writer);
                break;
            case ConnectionType.DeviceAuthRequest:
            case ConnectionType.UserDeviceAuthRequest:
                ThrowIfWrongMode(shouldBeHost: true);
                HandleAuthRequest(header, reader, writer, connectionHeader.MessageType);
                break;
            case ConnectionType.DeviceAuthResponse:
            case ConnectionType.UserDeviceAuthResponse:
                ThrowIfWrongMode(shouldBeHost: false);
                HandleAuthResponse(header, reader, writer);
                break;
            case ConnectionType.AuthDoneRequest:
                ThrowIfWrongMode(shouldBeHost: true);
                HandleAuthDoneRequest(header, reader, writer);
                break;
            case ConnectionType.AuthDoneRespone:
                ThrowIfWrongMode(shouldBeHost: false);
                HandleAuthDoneResponse(header, reader, writer);
                break;
            case ConnectionType.DeviceInfoMessage:
                HandleDeviceInfoMessage(header, reader, writer);
                break;
            default:
                throw UnexpectedMessage(connectionHeader.MessageType.ToString());
        }
    }

    void HandleConnectRequest(CommonHeader header, BinaryReader reader, BinaryWriter writer)
    {
        var connectionRequest = ConnectionRequest.Parse(reader);
        _remoteEncryption = CdpEncryptionInfo.FromRemote(connectionRequest.PublicKeyX, connectionRequest.PublicKeyY, connectionRequest.Nonce, CdpEncryptionParams.Default);

        var secret = _localEncryption.GenerateSharedSecret(_remoteEncryption);
        Cryptor = new(secret);

        header.SessionId |= (ulong)LocalSessionId << 32;

        header.Write(writer);

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
    }
    void HandleConnectResponse(CommonHeader header, BinaryReader reader, BinaryWriter writer)
    {
        var connectionResponse = ConnectionResponse.Parse(reader);
        _remoteEncryption = CdpEncryptionInfo.FromRemote(connectionResponse.PublicKeyX, connectionResponse.PublicKeyY, connectionResponse.Nonce, CdpEncryptionParams.Default);

        var secret = _localEncryption.GenerateSharedSecret(_remoteEncryption);
        Cryptor = new(secret);

        var sendAuthRequest = (ConnectionType type) =>
        {
            header.Flags = 0;
            SendMessage(writer, header, (writer) =>
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = type
                }.Write(writer);
                AuthenticationPayload.Create(
                    _localEncryption.DeviceCertificate!, // ToDo: User cert
                    _localEncryption.Nonce, _remoteEncryption!.Nonce
                ).Write(writer);
            });
        };
        sendAuthRequest(ConnectionType.DeviceAuthRequest);
        sendAuthRequest(ConnectionType.UserDeviceAuthRequest);
    }
    void HandleAuthRequest(CommonHeader header, BinaryReader reader, BinaryWriter writer, ConnectionType connectionType)
    {
        var authRequest = AuthenticationPayload.Parse(reader);
        if (!authRequest.VerifyThumbprint(_localEncryption.Nonce, _remoteEncryption!.Nonce))
            throw new CdpSecurityException("Invalid thumbprint");

        header.Flags = 0;
        SendMessage(writer, header, (writer) =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = connectionType == ConnectionType.DeviceAuthRequest ? ConnectionType.DeviceAuthResponse : ConnectionType.UserDeviceAuthResponse
            }.Write(writer);
            AuthenticationPayload.Create(
                _localEncryption.DeviceCertificate!, // ToDo: User cert
                _localEncryption.Nonce, _remoteEncryption!.Nonce
            ).Write(writer);
        });
    }
    void HandleAuthResponse(CommonHeader header, BinaryReader reader, BinaryWriter writer)
    {
        var authRequest = AuthenticationPayload.Parse(reader);
        if (!authRequest.VerifyThumbprint(_localEncryption.Nonce, _remoteEncryption!.Nonce))
            throw new CdpSecurityException("Invalid thumbprint");

        header.Flags = 0;
        SendMessage(writer, header, (writer) =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.AuthDoneRequest
            }.Write(writer);
            new HResultPayload()
            {
                HResult = 0 // No error
            }.Write(writer);
        });
    }
    void HandleAuthDoneRequest(CommonHeader header, BinaryReader reader, BinaryWriter writer)
    {
        header.Flags = 0;
        SendMessage(writer, header, (writer) =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.AuthDoneRespone // Ack
            }.Write(writer);
            new HResultPayload()
            {
                HResult = 0 // No error
            }.Write(writer);
        });
    }
    void HandleAuthDoneResponse(CommonHeader header, BinaryReader reader, BinaryWriter writer)
    {
        _ = HResultPayload.Parse(reader);
    }
    void HandleDeviceInfoMessage(CommonHeader header, BinaryReader reader, BinaryWriter writer)
    {
        var msg = DeviceInfoMessage.Parse(reader);

        header.Flags = 0;
        SendMessage(writer, header, (writer) =>
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
    void HandleControl(CommonHeader header, BinaryReader reader, BinaryWriter writer, CdpSocket socket)
    {
        if (Cryptor == null)
            throw UnexpectedMessage("Encryption");

        var controlHeader = ControlHeader.Parse(reader);
        Platform.Handler.Log(0, $"Received {header.Type} message {controlHeader.MessageType} from session {header.SessionId.ToString("X")} via {socket.TransportType}");
        switch (controlHeader.MessageType)
        {
            case ControlMessageType.StartChannelRequest:
                HandleStartChannelRequest(header, reader, writer, socket);
                break;
            default:
                throw UnexpectedMessage(controlHeader.MessageType.ToString());
        }
    }

    void HandleStartChannelRequest(CommonHeader header, BinaryReader reader, BinaryWriter writer, CdpSocket socket)
    {
        var request = StartChannelRequest.Parse(reader);

        header.AdditionalHeaders.Clear();
        header.SetReplyToId(header.RequestID);
        header.AdditionalHeaders.Add(new(
            (AdditionalHeaderType)129,
            new byte[] { 0x30, 0x0, 0x0, 0x1 }
        ));
        header.RequestID = 0;

        StartChannel(request, socket, out var channelId);

        header.Flags = 0;
        SendMessage(writer, header, (writer) =>
        {
            new ControlHeader()
            {
                MessageType = ControlMessageType.StartChannelResponse
            }.Write(writer);
            writer.Write((byte)0);
            writer.Write(channelId);
        });
    }
    #endregion

    void HandleSession(CommonHeader header, BinaryReader reader, BinaryWriter writer)
    {
        if (Cryptor == null)
            throw UnexpectedMessage("Encryption");

        CdpMessage msg = GetOrCreateMessage(header);
        msg.AddFragment(reader.ReadPayload());

        if (msg.IsComplete)
        {
            // NewSequenceNumber();
            _ = Task.Run(async () =>
            {
                try
                {
                    var channel = _channelRegistry.Get(header.ChannelId);
                    await channel.HandleMessageAsync(msg);
                }
                finally
                {
                    _msgRegistry.Remove(msg.Id, out _);
                    msg.Dispose();
                }
            });
        }
    }
    #endregion

    #region SequenceNumber
    uint _sequenceNumber = 0;
    internal uint NewSequenceNumber()
    {
        throw new NotImplementedException();
        // ToDo: Fix
        //lock (this)
        //    return _sequenceNumber++;
    }
    #endregion

    #region Messages
    readonly ConcurrentDictionary<uint, CdpMessage> _msgRegistry = new();
    CdpMessage GetOrCreateMessage(CommonHeader header)
        => _msgRegistry.GetOrAdd(header.SequenceNumber, (id) => new(header));
    #endregion

    #region Channels
    readonly AutoKeyRegistry<CdpChannel> _channelRegistry = new();

    void StartChannel(StartChannelRequest request, CdpSocket socket, out ulong channelId)
    {
        _channelRegistry.Create(channelId =>
        {
            var app = CdpAppRegistration.InstantiateApp(request.Id, request.Name);
            return new(this, channelId, app, socket);
        }, out channelId);
    }

    internal void UnregisterChannel(CdpChannel channel)
        => _channelRegistry.Remove(channel.ChannelId);
    #endregion

    Exception UnexpectedMessage(string? info = null)
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
