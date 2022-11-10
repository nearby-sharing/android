using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.DeviceInfo;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Control;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

/// <summary>
/// These messages are sent across during an active session between two connected and authenticated devices.
/// </summary>
public sealed class CdpSession : IDisposable
{
    public required uint LocalSessionId { get; init; }
    public required uint RemoteSessionId { get; init; }
    public required ICdpDeviceId DeviceId { get; init; }

    internal CdpSession() { }

    #region Registration
    static uint sessionIdCounter = 0xe;
    static Dictionary<uint, CdpSession> _registration = new();
    public static CdpSession GetOrCreate(ICdpDeviceId deviceId, CommonHeader header)
    {
        var sessionId = header.SessionId;
        var localSessionId = sessionId.HighValue();
        var remoteSessionId = sessionId.LowValue() & ~(uint)CommonHeader.SessionIdHostFlag;
        if (localSessionId != 0)
        {
            // Existing session            
            lock (_registration)
            {
                if (!_registration.ContainsKey(localSessionId))
                    throw new Exception("Session not found");

                var result = _registration[localSessionId];
                if (result.RemoteSessionId != remoteSessionId)
                    throw new Exception($"Wrong {nameof(RemoteSessionId)}");

                if (result.DeviceId.Address != deviceId.Address)
                    throw new Exception("Wrong device!");

                result.ThrowIfDisposed();

                return result;
            }
        }
        else
        {
            // Create
            localSessionId = sessionIdCounter++;
            CdpSession result = new()
            {
                DeviceId = deviceId,
                LocalSessionId = localSessionId,
                RemoteSessionId = remoteSessionId
            };
            _registration.Add(localSessionId, result);
            return result;
        }
    }
    #endregion

    public ICdpPlatformHandler? PlatformHandler { get; set; } = null;

    CdpCryptor? _cryptor = null;
    readonly CdpEncryptionInfo _localEncryption = CdpEncryptionInfo.Create(CdpEncryptionParams.Default);
    CdpEncryptionInfo? _remoteEncryption = null;
    public bool HandleMessage(CommonHeader header, BinaryReader reader, BinaryWriter writer)
    {
        ThrowIfDisposed();

        bool expectMessage = true;

        BinaryReader payloadReader = _cryptor?.Read(reader, header) ?? reader;
        {
            header.CorrectClientSessionBit();

            if (header.Type == MessageType.Connect)
            {
                ConnectionHeader connectionHeader = ConnectionHeader.Parse(payloadReader);
                PlatformHandler?.Log(0, $"Received {header.Type} message {connectionHeader.MessageType} from session {header.SessionId.ToString("X")}");
                switch (connectionHeader.MessageType)
                {
                    case ConnectionType.ConnectRequest:
                        {
                            var connectionRequest = ConnectionRequest.Parse(payloadReader);
                            _remoteEncryption = CdpEncryptionInfo.FromRemote(connectionRequest.PublicKeyX, connectionRequest.PublicKeyY, connectionRequest.Nonce, CdpEncryptionParams.Default);

                            var secret = _localEncryption.GenerateSharedSecret(_remoteEncryption);
                            _cryptor = new(secret);

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
                            break;
                        }
                    case ConnectionType.DeviceAuthRequest:
                    case ConnectionType.UserDeviceAuthRequest:
                        {
                            var authRequest = AuthenticationPayload.Parse(payloadReader);
                            if (!authRequest.VerifyThumbprint(_localEncryption.Nonce, _remoteEncryption!.Nonce))
                                throw new Exception("Invalid thumbprint");

                            header.Flags = 0;
                            _cryptor!.EncryptMessage(writer, header, (writer) =>
                            {
                                new ConnectionHeader()
                                {
                                    ConnectionMode = ConnectionMode.Proximal,
                                    MessageType = connectionHeader.MessageType == ConnectionType.DeviceAuthRequest ? ConnectionType.DeviceAuthResponse : ConnectionType.UserDeviceAuthResponse
                                }.Write(writer);
                                AuthenticationPayload.Create(
                                    _localEncryption.DeviceCertificate!, // ToDo: User cert
                                    _localEncryption.Nonce, _remoteEncryption!.Nonce
                                ).Write(writer);
                            });
                            break;
                        }
                    case ConnectionType.UpgradeRequest:
                        {
                            header.Flags = 0;
                            _cryptor!.EncryptMessage(writer, header, (writer) =>
                            {
                                new ConnectionHeader()
                                {
                                    ConnectionMode = ConnectionMode.Proximal,
                                    MessageType = ConnectionType.UpgradeFailure // We currently only support BT
                                }.Write(writer);
                                new HResultPayload()
                                {
                                    HResult = 1 // Failure: Anything != 0
                                }.Write(writer);
                            });
                            break;
                        }
                    case ConnectionType.AuthDoneRequest:
                        {
                            header.Flags = 0;
                            _cryptor!.EncryptMessage(writer, header, (writer) =>
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
                            break;
                        }
                    case ConnectionType.DeviceInfoMessage:
                        {
                            var msg = DeviceInfoMessage.Parse(payloadReader);

                            header.Flags = 0;
                            _cryptor!.EncryptMessage(writer, header, (writer) =>
                            {
                                new ConnectionHeader()
                                {
                                    ConnectionMode = ConnectionMode.Proximal,
                                    MessageType = ConnectionType.DeviceInfoResponseMessage // Ack
                                }.Write(writer);
                            });
                            break;
                        }
                    default:
                        throw UnexpectedMessage();
                }
            }
            else if (header.Type == MessageType.Control)
            {
                var controlHeader = ControlHeader.Parse(payloadReader);
                PlatformHandler?.Log(0, $"Received {header.Type} message {controlHeader.MessageType} from session {header.SessionId.ToString("X")}");
                switch (controlHeader.MessageType)
                {
                    case ControlMessageType.StartChannelRequest:
                        {
                            var request = StartChannelRequest.Parse(payloadReader);

                            header.AdditionalHeaders.Clear();
                            header.SetReplyToId(header.RequestID);
                            header.AdditionalHeaders.Add(new(
                                (AdditionalHeaderType)129,
                                new byte[] { 0x30, 0x0, 0x0, 0x1 }
                            ));

                            header.RequestID = 0;

                            header.Flags = 0;
                            _cryptor!.EncryptMessage(writer, header, (writer) =>
                            {
                                new ControlHeader()
                                {
                                    MessageType = ControlMessageType.StartChannelResponse
                                }.Write(writer);
                                writer.Write((byte)0);
                                writer.Write(StartChannel(request));
                            });
                            break;
                        }
                    default:
                        throw UnexpectedMessage();
                }
            }
            else if (header.Type == MessageType.Session)
            {
                CdpMessage msg = GetOrCreateMessage(header);
                msg.AddFragment(payloadReader.ReadPayload());

                if (msg.IsComplete)
                {
                    try
                    {
                        var channel = _channelRegistry[header.ChannelId];

                        bool channelExpectMessage = true;

                        header.Flags = 0;
                        _cryptor!.EncryptMessage(writer, header, (payloadWriter) =>
                        {
                            channelExpectMessage = channel.HandleMessage(this, msg, payloadWriter);
                        });

                        if (!channelExpectMessage)
                        {
                            _channelRegistry.Remove(header.ChannelId);
                            channel.Dispose();
                        }
                    }
                    finally
                    {
                        _msgRegistry.Remove(msg.Id);
                        msg.Dispose();
                    }
                }
            }
            else
            {
                // We might receive a "ReliabilityResponse"
                // ignore
                PlatformHandler?.Log(0, $"Received {header.Type} message from session {header.SessionId.ToString("X")}");
            }
        }

        writer.Flush();

        if (IsDisposed)
            return false;
        return expectMessage;
    }

    #region Messages
    readonly Dictionary<uint, CdpMessage> _msgRegistry = new();

    CdpMessage GetOrCreateMessage(CommonHeader header)
    {
        if (_msgRegistry.TryGetValue(header.SequenceNumber, out var result))
            return result;

        result = new(header);
        _msgRegistry.Add(header.SequenceNumber, result);
        return result;
    }
    #endregion

    #region Channels
    ulong channelCounter = 1;
    readonly Dictionary<ulong, ICdpApp> _channelRegistry = new();

    ulong StartChannel(StartChannelRequest request)
    {
        lock (_channelRegistry)
        {
            var app = CdpAppRegistration.InstantiateApp(request.Id, request.Name);
            _channelRegistry.Add(channelCounter, app);
            return channelCounter++;
        }
    }
    #endregion

    Exception UnexpectedMessage(string? info = null)
        => new SecurityException($"Recieved unexpected message {info ?? "null"}");

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

        lock (_registration)
        {
            _registration.Remove(LocalSessionId);
        }

        foreach (var channel in _channelRegistry.Values)
            channel.Dispose();
        _channelRegistry.Clear();

        foreach (var msg in _msgRegistry.Values)
            msg.Dispose();
        _msgRegistry.Clear();
    }
}
