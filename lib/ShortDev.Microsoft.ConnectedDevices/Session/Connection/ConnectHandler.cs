using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.DeviceInfo;
using ShortDev.Microsoft.ConnectedDevices.Session.Upgrade;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Session.Connection;
internal abstract class ConnectHandler(CdpSession session, UpgradeHandler upgradeHandler)
{
    readonly ILogger<ConnectHandler> _logger = session.Platform.CreateLogger<ConnectHandler>();
    protected readonly CdpSession _session = session;
    protected readonly CdpEncryptionInfo _localEncryption = CdpEncryptionInfo.Create(CdpEncryptionParams.Default);
    protected CdpEncryptionInfo? _remoteEncryption = null;

    internal UpgradeHandler UpgradeHandler { get; } = upgradeHandler;

    public void HandleConnect(CdpSocket socket, CommonHeader header, ref HeapEndianReader reader)
    {
        ConnectionHeader connectionHeader = ConnectionHeader.Parse(ref reader);
        _logger.ReceivedConnectMessage(
            connectionHeader.MessageType,
            header.SessionId,
            socket.TransportType
        );

        if (UpgradeHandler.TryHandleConnect(socket, connectionHeader, ref reader))
            return;

        if (!UpgradeHandler.IsSocketAllowed(socket))
            throw CdpSession.UnexpectedMessage(socket.Endpoint.Address);

        HandleMessageInternal(socket, header, connectionHeader, ref reader);
    }

    protected abstract void HandleMessageInternal(CdpSocket socket, CommonHeader header, ConnectionHeader connectionHeader, ref HeapEndianReader reader);

    public CdpDeviceInfo? DeviceInfo { get; private set; }
    protected void HandleDeviceInfoMessage(CommonHeader header, ref HeapEndianReader reader, CdpSocket socket)
    {
        var msg = DeviceInfoMessage.Parse(ref reader);
        _logger.ReceivedDeviceInfo(msg.DeviceInfo);

        DeviceInfo = msg.DeviceInfo;

        var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
        try
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.DeviceInfoResponseMessage // Ack
            }.Write(ref writer);

            header.Flags = 0;
            _session.SendMessage(socket, header, writer.Stream.WrittenSpan);
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static ConnectHandler Create(CdpSession session, EndpointInfo initialEndpoint)
        => session.SessionId.IsHost switch
        {
            true => new HostConnectHandler(session, new(session, initialEndpoint)),
            false => new ClientConnectHandler(session, new(session, initialEndpoint))
        };
}
