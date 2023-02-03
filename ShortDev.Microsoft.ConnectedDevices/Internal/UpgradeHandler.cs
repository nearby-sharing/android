﻿using ShortDev.Microsoft.ConnectedDevices.Messages;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection;
using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using ShortDev.Networking;
using System;
using System.IO;
using System.Linq;

namespace ShortDev.Microsoft.ConnectedDevices.Internal;

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

    public bool HandleConnect(CdpSocket socket, CommonHeader header, ConnectionHeader connectionHeader, BinaryReader reader, EndianWriter writer)
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

    readonly ConcurrentList<Guid> _upgradeIds = new();
    void HandleTransportRequest(CdpSocket socket, CommonHeader header, BinaryReader reader, EndianWriter writer)
    {
        var msg = TransportRequest.Parse(reader);

        // Sometimes the device sends multiple transport requests
        // If we know it already then let it pass
        bool allowed = IsSocketAllowed(socket);
        if (!allowed && _upgradeIds.Contains(msg.UpgradeId))
        {
            // No we have confirmed that this address belongs to the same device (different transport)
            _allowedAddresses.Add(socket.RemoteDevice.Address);
            _upgradeIds.Remove(msg.UpgradeId);

            allowed = true;
        }

        _session.Platform.Handler.Log(0, $"Transport upgrade {msg.UpgradeId} {(allowed ? "succeeded" : "failed")}");

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

    void HandleUpgradeRequest(CommonHeader header, BinaryReader reader, EndianWriter writer)
    {
        var msg = UpgradeRequest.Parse(reader);
        _session.Platform.Handler.Log(0, $"Upgrade request {msg.UpgradeId} to {string.Join(',', msg.Endpoints.Select((x) => x.Type.ToString()))}");

        var networkTransport = _session.Platform.TryGetTransport<NetworkTransport>();
        if (networkTransport == null)
        {
            _session.Cryptor!.EncryptMessage(writer, header, (writer) =>
            {
                new ConnectionHeader()
                {
                    ConnectionMode = ConnectionMode.Proximal,
                    MessageType = ConnectionType.UpgradeFailure
                }.Write(writer);
                new HResultPayload()
                {
                    HResult = -1
                }.Write(writer);
            });
            return;
        }

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
                    new HostEndpointMetadata(CdpTransportType.Tcp, networkTransport.Handler.GetLocalIp(), Constants.TcpPort.ToString())
                },
                Endpoints = new[]
                {
                    TransportEndpoint.Tcp
                }
            }.Write(writer);
        });
    }

    void HandleUpgradeFinalization(CommonHeader header, BinaryReader reader, EndianWriter writer)
    {
        var msg = TransportEndpoint.ParseArray(reader);
        _session.Platform.Handler.Log(0, $"Transport upgrade to {string.Join(',', msg.Select((x) => x.Type.ToString()))}");

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

    void HandleUpgradeFailure(CommonHeader header, BinaryReader reader, EndianWriter writer)
    {
        var msg = HResultPayload.Parse(reader);
        _session.Platform.Handler.Log(0, $"Transport upgrade failed with HResult {msg.HResult}");
    }
}
