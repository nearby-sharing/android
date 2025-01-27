using Microsoft.Extensions.Logging;
using ShortDev.Microsoft.ConnectedDevices.Internal;
using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using ShortDev.Microsoft.ConnectedDevices.Transports.Network;

namespace ShortDev.Microsoft.ConnectedDevices.Session.Upgrade;
internal sealed class HostUpgradeHandler(CdpSession session, EndpointInfo initialEndpoint) : UpgradeHandler(session, initialEndpoint)
{
    readonly ILogger _logger = session.Platform.CreateLogger<HostUpgradeHandler>();

    protected override bool TryHandleConnectInternal(CdpSocket socket, ConnectionHeader connectionHeader, ref EndianReader reader)
    {
        // This part needs to be always accessible!
        // This is used to validate
        if (connectionHeader.MessageType == ConnectionType.TransportRequest)
        {
            HandleTransportRequest(socket, ref reader);
            return true;
        }

        // If invalid socket, return false and let CdpSession.HandleConnect throw
        if (!IsSocketAllowed(socket))
            return false;

        switch (connectionHeader.MessageType)
        {
            // Host
            case ConnectionType.UpgradeRequest:
                HandleUpgradeRequest(socket, ref reader);
                return true;

            case ConnectionType.UpgradeFinalization:
                HandleUpgradeFinalization(socket, ref reader);
                return true;

            case ConnectionType.UpgradeFailure:
                return true;
        }
        return false;
    }

    readonly SynchronizedList<Guid> _upgradeIds = [];
    void HandleTransportRequest(CdpSocket socket, ref EndianReader reader)
    {
        var msg = UpgradeIdPayload.Parse(ref reader);

        // Sometimes the device sends multiple transport requests
        // If we know it already then let it pass
        bool allowed = IsSocketAllowed(socket);
        if (!allowed && _upgradeIds.Contains(msg.UpgradeId))
        {
            // No we have confirmed that this address belongs to the same device (different transport)
            _allowedAddresses.Add(socket.Endpoint.Address);
            _upgradeIds.Remove(msg.UpgradeId);

            allowed = true;
        }

        _logger.UpgradeTransportRequest(
            msg.UpgradeId,
            allowed ? "succeeded" : "failed"
        );

        CommonHeader header = new()
        {
            Type = MessageType.Connect
        };

        using var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
        new ConnectionHeader()
        {
            ConnectionMode = ConnectionMode.Proximal,
            MessageType = allowed ? ConnectionType.TransportConfirmation : ConnectionType.UpgradeFailure
        }.Write(writer);
        msg.Write(writer);

        _session.SendMessage(socket, header, writer);

        RemoteEndpoint = socket.Endpoint;
    }

    void HandleUpgradeRequest(CdpSocket socket, ref EndianReader reader)
    {
        var msg = UpgradeRequest.Parse(ref reader);
        _logger.UpgradeRequest(
            msg.UpgradeId,
            msg.Endpoints.Select((x) => x.Type)
        );

        CommonHeader header = new()
        {
            Type = MessageType.Connect
        };

        var networkTransport = _session.Platform.TryGetTransport<NetworkTransport>();
        var localIp = networkTransport?.Handler.TryGetLocalIp();
        if (networkTransport == null || localIp == null)
        {
            using var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.UpgradeFailure
            }.Write(writer);
            new HResultPayload()
            {
                HResult = -1
            }.Write(writer);

            _session.SendMessage(socket, header, writer);
            return;
        }

        _upgradeIds.Add(msg.UpgradeId);

        using (var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool))
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.UpgradeResponse
            }.Write(writer);
            new UpgradeResponse()
            {
                Endpoints =
                [
                    EndpointInfo.FromTcp(localIp, networkTransport.TcpPort)
                ],
                MetaData =
                [
                    EndpointMetadata.Tcp
                ]
            }.Write(writer);

            _session.SendMessage(socket, header, writer);
        }
    }

    void HandleUpgradeFinalization(CdpSocket socket, ref EndianReader reader)
    {
        var msg = EndpointMetadata.ParseArray(ref reader);
        _logger.UpgradeFinalization(
            msg.Select((x) => x.Type)
        );

        CommonHeader header = new()
        {
            Type = MessageType.Connect
        };

        using var writer = EndianWriter.Create(Endianness.BigEndian, ConnectedDevicesPlatform.MemoryPool);
        new ConnectionHeader()
        {
            ConnectionMode = ConnectionMode.Proximal,
            MessageType = ConnectionType.UpgradeFinalizationResponse
        }.Write(writer);

        _session.SendMessage(socket, header, writer);
    }
}
