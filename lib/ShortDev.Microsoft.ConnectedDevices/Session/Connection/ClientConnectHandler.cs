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

        EndianWriter writer = new(Endianness.BigEndian);
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
            MessageFragmentSize = MessageFragmenter.DefaultMessageFragmentSize,
            Nonce = _localEncryption.Nonce,
            PublicKeyX = publicKey.X!,
            PublicKeyY = publicKey.Y!
        }.Write(writer);

        _session.SendMessage(socket, header, writer);

        await _promise;
    }

    protected override void HandleMessageInternal(CdpSocket socket, CommonHeader header, ConnectionHeader connectionHeader, ref EndianReader reader)
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

    void HandleConnectResponse(CommonHeader header, ref EndianReader reader, CdpSocket socket)
    {
        _session.HostCapabilities = (PeerCapabilities)(header.TryGetHeader(AdditionalHeaderType.PeerCapabilities)?.AsUInt64() ?? 0);

        var connectionResponse = ConnectionResponse.Parse(ref reader);
        _remoteEncryption = CdpEncryptionInfo.FromRemote(connectionResponse.PublicKeyX, connectionResponse.PublicKeyY, connectionResponse.Nonce, CdpEncryptionParams.Default);

        var secret = _localEncryption.GenerateSharedSecret(_remoteEncryption);
        _session.Cryptor = new(secret);

        EndianWriter writer = new(Endianness.BigEndian);
        new ConnectionHeader()
        {
            ConnectionMode = ConnectionMode.Proximal,
            MessageType = ConnectionType.DeviceAuthRequest
        }.Write(writer);
        AuthenticationPayload.Create(
            _session.Platform.DeviceInfo.DeviceCertificate!, // ToDo: User cert
            hostNonce: _remoteEncryption!.Nonce, clientNonce: _localEncryption.Nonce
        ).Write(writer);

        header.Flags = 0;
        _session.SendMessage(socket, header, writer);
    }

    void HandleAuthResponse(CommonHeader header, ref EndianReader reader, CdpSocket socket, ConnectionType connectionType)
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

            try
            {
                EndianWriter writer = new(Endianness.BigEndian);
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.AuthDoneRequest
                }.Write(writer);

                header.Flags = 0;
                _session.SendMessage(socket, header, writer);
            }
            catch (Exception ex)
            {
                _promise?.TrySetException(ex);
            }
        }
    }

    void HandleDeviceAuthResponse(CdpSocket socket, CommonHeader header)
    {
        EndianWriter writer = new(Endianness.BigEndian);
        new ConnectionHeader()
        {
            ConnectionMode = ConnectionMode.Proximal,
            MessageType = ConnectionType.UserDeviceAuthRequest
        }.Write(writer);
        AuthenticationPayload.Create(
            _session.Platform.DeviceInfo.DeviceCertificate!, // ToDo: User cert
            hostNonce: _remoteEncryption!.Nonce, clientNonce: _localEncryption.Nonce
        ).Write(writer);

        header.Flags = 0;
        _session.SendMessage(socket, header, writer);
    }

    void HandleAuthDoneResponse(CdpSocket socket, ref EndianReader reader)
    {
        var msg = ResultPayload.Parse(ref reader);
        msg.ThrowOnError();

        EndianWriter writer = new(Endianness.BigEndian);
        new ConnectionHeader()
        {
            ConnectionMode = ConnectionMode.Proximal,
            MessageType = ConnectionType.DeviceInfoMessage
        }.Write(writer);
        new DeviceInfoMessage()
        {
            DeviceInfo = _session.Platform.GetCdpDeviceInfo()
        }.Write(writer);

        _session.SendMessage(
            socket,
            new CommonHeader()
            {
                Type = MessageType.Connect
            },
            writer
        );

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
