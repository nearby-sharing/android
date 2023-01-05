using ShortDev.Microsoft.ConnectedDevices.Protocol.Messages;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Transports;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Internal;

internal sealed class UpgradeHandler
{
    CdpSession _session;
    public UpgradeHandler(CdpSession session, CdpDevice initalDevice)
    {
        _session = session;

        // Initial address is always allowed
        _allowedAddresses.Add(initalDevice.Address);
    }

    ConcurrentList<string> _allowedAddresses = new();
    public bool IsSocketAllowed(CdpSocket socket)
        => _allowedAddresses.Contains(socket.RemoteDevice.Address);

    public bool HandleConnect(CdpSocket socket, CommonHeader header, ConnectionHeader connectionHeader, BinaryReader reader, BinaryWriter writer)
    {
        // This part need to be always accessible!
        // This is used to validate
        if (connectionHeader.MessageType == ConnectionType.TransportRequest)
        {
            HandleTransportRequest(socket, header, reader, writer);
            return true;
        }

        // If invalid socket return false and let CdpSession.HandleConnect throw
        if (!IsSocketAllowed(socket))
            return false;

        switch (connectionHeader.MessageType)
        {
            case ConnectionType.UpgradeRequest:
                HandleUpgradeRequest(header, reader, writer);
                return true;
            case ConnectionType.UpgradeFinalization:
                HandleUpgradeFinalization(header, reader, writer);
                return true;
            case ConnectionType.UpgradeFailure:
                HandleUpgradeFailure(header, reader, writer);
                return true;
        }
        return false;
    }

    ConcurrentList<Guid> _upgradeIds = new();
    void HandleTransportRequest(CdpSocket socket, CommonHeader header, BinaryReader reader, BinaryWriter writer)
    {
        var msg = TransportRequest.Parse(reader);

        bool allowed = _upgradeIds.Contains(msg.UpgradeId);
        if (allowed)
        {
            // No we have confirmed that this address belongs to the same device (different transport)
            _allowedAddresses.Add(socket.RemoteDevice.Address);
            _upgradeIds.Remove(msg.UpgradeId);
        }

        _session.PlatformHandler?.Log(0, $"Transport upgrade {msg.UpgradeId} {(allowed ? "succeeded" : "failed")}");

        header.Flags = 0;
        _session.Cryptor!.EncryptMessage(writer, header, (writer) =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = allowed ? ConnectionType.TransportConfirmation : ConnectionType.UpgradeFailure
            }.Write(writer);
            msg.Write(writer);
        });
    }

    void HandleUpgradeRequest(CommonHeader header, BinaryReader reader, BinaryWriter writer)
    {
        var msg = UpgradeRequest.Parse(reader);
        _session.PlatformHandler?.Log(0, $"Upgrade request {msg.UpgradeId} to {string.Join(',', msg.Endpoints.Select((x) => x.Type.ToString()))}");

        _upgradeIds.Add(msg.UpgradeId);

        header.Flags = 0;
        _session.Cryptor!.EncryptMessage(writer, header, (writer) =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.UpgradeResponse
            }.Write(writer);
            new UpgradeResponse()
            {
                HostEndpoints = new[]
                {
                    new HostEndpointMetadata(CdpTransportType.Tcp, _session.PlatformHandler!.GetLocalIP(), Constants.TcpPort.ToString())
                },
                Endpoints = new[]
                {
                    TransportEndpoint.Tcp
                }
            }.Write(writer);
        });
    }
    void HandleUpgradeFinalization(CommonHeader header, BinaryReader reader, BinaryWriter writer)
    {
        var msg = TransportEndpoint.ParseArray(reader);
        _session.PlatformHandler?.Log(0, $"Transport upgrade to {string.Join(',', msg.Select((x) => x.Type.ToString()))}");

        header.Flags = 0;
        _session.Cryptor!.EncryptMessage(writer, header, (writer) =>
        {
            new ConnectionHeader()
            {
                ConnectionMode = ConnectionMode.Proximal,
                MessageType = ConnectionType.UpgradeFinalizationResponse
            }.Write(writer);
        });
    }
    void HandleUpgradeFailure(CommonHeader header, BinaryReader reader, BinaryWriter writer)
    {
        var msg = HResultPayload.Parse(reader);
        _session.PlatformHandler?.Log(0, $"Transport upgrade failed with HResult {msg.HResult}");
    }
}
