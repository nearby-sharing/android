﻿using ShortDev.Networking;
using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

/// <summary>
/// This message transports the upgrade request.
/// </summary>
public sealed class UpgradeRequest : ICdpPayload<UpgradeRequest>
{
    /// <summary>
    /// A random GUID identifying this upgrade process across transports.
    /// </summary>
    public required Guid UpgradeId { get; init; }
    public required TransportEndpoint[] Endpoints { get; init; }

    public static UpgradeRequest Parse(EndianReader reader)
        => new()
        {
            UpgradeId = new(reader.ReadBytes(16)),
            Endpoints = TransportEndpoint.ParseArray(reader)
        };

    public void Write(EndianWriter writer)
    {
        writer.Write(UpgradeId.ToByteArray());
        TransportEndpoint.WriteArray(writer, Endpoints);
    }
}