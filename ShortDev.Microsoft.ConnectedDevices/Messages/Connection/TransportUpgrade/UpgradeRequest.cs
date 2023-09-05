using ShortDev.Networking;
using System;

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
    public required EndpointMetadata[] Endpoints { get; init; }

    public static UpgradeRequest Parse(ref EndianReader reader)
        => new()
        {
            UpgradeId = reader.ReadGuid(),
            Endpoints = EndpointMetadata.ParseArray(ref reader)
        };

    public void Write(EndianWriter writer)
    {
        writer.Write(UpgradeId.ToByteArray());
        EndpointMetadata.WriteArray(writer, Endpoints);
    }
}