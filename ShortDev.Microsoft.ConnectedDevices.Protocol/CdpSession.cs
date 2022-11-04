using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.DeviceInfo;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Control;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Serialization;
using ShortDev.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public void HandleMessage(CdpRfcommSocket socket, CommonHeader header, BinaryReader reader, BinaryWriter writer, ref bool expectMessage)
    {
        ThrowIfDisposed();

        BinaryReader payloadReader = _cryptor?.Read(reader, header) ?? reader;
        {
            header.CorrectClientSessionBit();

            if (header.Type == MessageType.Connect)
            {
                ConnectionHeader connectionHeader = ConnectionHeader.Parse(payloadReader);
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
                switch (controlHeader.MessageType)
                {
                    case ControlMessageType.StartChannelRequest:
                        {
                            var msg = StartChannelRequest.Parse(payloadReader);

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
                                writer.Write(BinaryConvert.ToBytes("000000000000000001")); // 000000000000000001
                            });
                            break;
                        }
                    default:
                        throw UnexpectedMessage();
                }
            }
            else if (header.Type == MessageType.Session)
            {
                var prepend = payloadReader.ReadBytes(0x0000000C);
                var buffer = payloadReader.ReadPayload();
                var payload = ValueSet.Parse(buffer);
                Debug.Print(BinaryConvert.ToString(buffer));
                header.AdditionalHeaders.RemoveAll((x) => x.Type == AdditionalHeaderType.CorrelationVector);

                ValueSet response = new();
                if (payload.ContainsKey("Uri"))
                {
                    PlatformHandler?.LaunchUri(payload.Get<string>("Uri"));
                    expectMessage = false;
                    response.Add("ControlMessage", 2u);
                }
                else
                {
                    response.Add("SelectedPlatformVersion", 1u);
                    response.Add("VersionHandShakeResult", 1u);
                }

                header.Flags = 0;
                _cryptor!.EncryptMessage(writer, header, (payloadWriter) =>
                {
                    payloadWriter.Write(prepend);
                    response.Write(payloadWriter);
                });
            }
            else
            {
                // We might receive a "ReliabilityResponse"
                // ignore
            }
        }

        writer.Flush();
    }

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
        IsDisposed = true;
    }
}
