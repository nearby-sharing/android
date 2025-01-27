using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Exceptions;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.Authentication;
using ShortDev.Microsoft.ConnectedDevices.Session.Upgrade;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Session.Connection;
internal sealed class HostConnectHandler(CdpSession session, HostUpgradeHandler upgradeHandler) : ConnectHandler(session, upgradeHandler)
{
    protected override void HandleMessageInternal(CdpSocket socket, CommonHeader header, ConnectionHeader connectionHeader, ref EndianReader reader)
    {
        if (connectionHeader.MessageType == ConnectionType.ConnectRequest)
        {
            if (_session.Cryptor != null)
                throw CdpSession.UnexpectedMessage("Encryption");

            HandleConnectRequest(header, ref reader, socket);
            return;
        }

        if (_session.Cryptor == null)
            throw CdpSession.UnexpectedMessage("Encryption");

        switch (connectionHeader.MessageType)
        {
            case ConnectionType.DeviceAuthRequest:
            case ConnectionType.UserDeviceAuthRequest:
                HandleAuthRequest(header, ref reader, socket, connectionHeader.MessageType);
                break;

            case ConnectionType.AuthDoneRequest:
                HandleAuthDoneRequest(header, socket);
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
        _remoteEncryption = CdpEncryptionInfo.FromRemote(connectionRequest.PublicKeyX, connectionRequest.PublicKeyY, connectionRequest.Nonce, CdpEncryptionParams.Default);

        using (var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool))
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

            _session.SendMessage(socket, header, writer);
        }

        // We have to set cryptor after we send the message because it would be encrypted otherwise
        var secret = _localEncryption.GenerateSharedSecret(_remoteEncryption);
        _session.Cryptor = new(secret);
    }

    void HandleAuthRequest(CommonHeader header, ref EndianReader reader, CdpSocket socket, ConnectionType connectionType)
    {
        var authRequest = AuthenticationPayload.Parse(ref reader);
        if (!authRequest.VerifyThumbprint(hostNonce: _localEncryption.Nonce, clientNonce: _remoteEncryption!.Nonce))
            throw new CdpSecurityException("Invalid thumbprint");

        using var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
        new ConnectionHeader()
        {
            ConnectionMode = ConnectionMode.Proximal,
            MessageType = connectionType == ConnectionType.DeviceAuthRequest ? ConnectionType.DeviceAuthResponse : ConnectionType.UserDeviceAuthResponse
        }.Write(writer);
        AuthenticationPayload.Create(
            _session.Platform.DeviceInfo.DeviceCertificate, // ToDo: User cert
            hostNonce: _localEncryption.Nonce, clientNonce: _remoteEncryption!.Nonce
        ).Write(writer);

        header.Flags = 0;
        _session.SendMessage(socket, header, writer);
    }

    void HandleAuthDoneRequest(CommonHeader header, CdpSocket socket)
    {
        using var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
        new ConnectionHeader()
        {
            ConnectionMode = ConnectionMode.Proximal,
            MessageType = ConnectionType.AuthDoneRespone // Ack
        }.Write(writer);
        new ResultPayload()
        {
            Result = CdpResult.Success
        }.Write(writer);

        header.Flags = 0;
        _session.SendMessage(socket, header, writer);
    }
}
