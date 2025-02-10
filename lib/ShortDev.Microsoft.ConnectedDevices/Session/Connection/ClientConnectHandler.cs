using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;
using ShortDev.Microsoft.ConnectedDevices.Session.Upgrade;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Runtime.CompilerServices;

namespace ShortDev.Microsoft.ConnectedDevices.Session.Connection;
internal sealed class ClientConnectHandler(CdpSession session, ClientUpgradeHandler upgradeHandler) : ConnectHandler(session, upgradeHandler)
{
    readonly ClientUpgradeHandler _clientUpgradeHandler = upgradeHandler;
    readonly ILogger _logger = session.Platform.CreateLogger<ClientConnectHandler>();

    ConnectionTask? _promise;
    public async Task ConnectAsync(CdpSocket socket, bool upgradeSupported = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.CompareExchange(ref _promise, new(cancellationToken), null) is not null)
            throw new InvalidOperationException("Already connecting");

        CommonHeader header = new()
        {
            Type = MessageType.Connect,
            AdditionalHeaders = {
                AdditionalHeader.FromUInt32(AdditionalHeaderType.Header129, 0x70_00_00_03),
                AdditionalHeader.FromUInt64(AdditionalHeaderType.PeerCapabilities, (ulong)PeerCapabilities.All),
                AdditionalHeader.FromUInt64(AdditionalHeaderType.Header131, upgradeSupported ? 7u : 6u)
            }
        };

        var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
        try
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.ConnectRequest
            }.Write(ref writer);

            var publicKey = _localEncryption.PublicKey;
            new ConnectionRequest()
            {
                CurveType = CurveType.CT_NIST_P256_KDF_SHA512,
                HmacSize = Constants.HMacSize,
                MessageFragmentSize = MessageFragmenter.DefaultMessageFragmentSize,
                Nonce = _localEncryption.Nonce,
                PublicKeyX = publicKey.X!,
                PublicKeyY = publicKey.Y!
            }.Write(ref writer);

            _session.SendMessage(socket, header, writer.Stream.WrittenSpan);
        }
        finally
        {
            writer.Dispose();
        }

        await _promise;
    }

    protected override void HandleMessageInternal(CdpSocket socket, CommonHeader header, ConnectionHeader connectionHeader, ref HeapEndianReader reader)
    {
        if (_promise?.CancellationToken.IsCancellationRequested == true)
            return;

        if (connectionHeader.MessageType == ConnectionType.ConnectResponse)
        {
            if (_session.Cryptor != null)
                throw CdpSession.UnexpectedMessage("Encryption");

            HandleConnectResponse(header, ref reader, socket);
            return;
        }

        if (_session.Cryptor == null)
            throw CdpSession.UnexpectedMessage("Encryption");

        switch (connectionHeader.MessageType)
        {
            case ConnectionType.DeviceAuthResponse:
            case ConnectionType.UserDeviceAuthResponse:
                HandleAuthResponse(header, ref reader, socket, connectionHeader.MessageType);
                break;

            case ConnectionType.AuthDoneRespone:
                HandleAuthDoneResponse(socket, ref reader);
                break;

            case ConnectionType.DeviceInfoMessage:
                HandleDeviceInfoMessage(header, ref reader, socket);
                break;

            case ConnectionType.DeviceInfoResponseMessage:
                break;
        }
    }

    void HandleConnectResponse(CommonHeader header, ref HeapEndianReader reader, CdpSocket socket)
    {
        _session.HostCapabilities = (PeerCapabilities)(header.TryGetHeader(AdditionalHeaderType.PeerCapabilities)?.AsUInt64() ?? 0);

        var connectionResponse = ConnectionResponse.Parse(ref reader);
        _remoteEncryption = CdpEncryptionInfo.FromRemote(connectionResponse.PublicKeyX, connectionResponse.PublicKeyY, connectionResponse.Nonce, CdpEncryptionParams.Default);

        var secret = _localEncryption.GenerateSharedSecret(_remoteEncryption);
        _session.Cryptor = new(secret);

        var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
        try
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.DeviceAuthRequest
            }.Write(ref writer);
            AuthenticationPayload.Create(
                _session.Platform.DeviceInfo.DeviceCertificate!, // ToDo: User cert
                hostNonce: _remoteEncryption!.Nonce, clientNonce: _localEncryption.Nonce
            ).Write(ref writer);

            header.Flags = 0;
            _session.SendMessage(socket, header, writer.Stream.WrittenSpan);
        }
        finally
        {
            writer.Dispose();
        }
    }

    void HandleAuthResponse(CommonHeader header, ref HeapEndianReader reader, CdpSocket socket, ConnectionType connectionType)
    {
        var authRequest = AuthenticationPayload.Parse(ref reader);
        if (!authRequest.VerifyThumbprint(hostNonce: _remoteEncryption!.Nonce, clientNonce: _localEncryption.Nonce))
            throw new CdpSecurityException("Invalid thumbprint");

        if (connectionType == ConnectionType.DeviceAuthResponse)
        {
            HandleDeviceAuthResponse(socket, header);
            return;
        }

        PrepareSession(socket);

        async void PrepareSession(CdpSocket socket)
        {
            if (socket.TransportType == CdpTransportType.Rfcomm)
            {
                try
                {
                    var oldSocket = socket;
                    socket = await _clientUpgradeHandler.UpgradeAsync(oldSocket);
                    oldSocket.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.UpgradeFailed(ex);
                }
            }

            var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
            try
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.AuthDoneRequest
                }.Write(ref writer);

                header.Flags = 0;
                _session.SendMessage(socket, header, writer.Stream.WrittenSpan);
            }
            catch (Exception ex)
            {
                _promise?.TrySetException(ex);
            }
            finally
            {
                writer.Dispose();
            }
        }
    }

    void HandleDeviceAuthResponse(CdpSocket socket, CommonHeader header)
    {
        var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
        try
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.UserDeviceAuthRequest
            }.Write(ref writer);
            AuthenticationPayload.Create(
                _session.Platform.DeviceInfo.DeviceCertificate!, // ToDo: User cert
                hostNonce: _remoteEncryption!.Nonce, clientNonce: _localEncryption.Nonce
            ).Write(ref writer);

            header.Flags = 0;
            _session.SendMessage(socket, header, writer.Stream.WrittenSpan);
        }
        finally
        {
            writer.Dispose();
        }
    }

    void HandleAuthDoneResponse(CdpSocket socket, ref HeapEndianReader reader)
    {
        var msg = ResultPayload.Parse(ref reader);
        msg.ThrowOnError();

        var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
        try
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.DeviceInfoMessage
            }.Write(ref writer);
            new DeviceInfoMessage()
            {
                DeviceInfo = _session.Platform.GetCdpDeviceInfo()
            }.Write(ref writer);

            _session.SendMessage(
                socket,
                new CommonHeader()
                {
                    Type = MessageType.Connect
                },
                writer.Stream.WrittenSpan
            );
        }
        finally
        {
            writer.Dispose();
        }

        _promise?.TrySetResult();
    }

    sealed class ConnectionTask
    {
        readonly TaskCompletionSource _promise = new();

        public CancellationToken CancellationToken { get; }
        public ConnectionTask(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;

            cancellationToken.Register(() => _promise.TrySetCanceled());
        }

        public void TrySetResult()
            => _promise.TrySetResult();

        public void TrySetException(Exception ex)
            => _promise.TrySetException(ex);

        public TaskAwaiter GetAwaiter()
            => _promise.Task.GetAwaiter();
    }
}
