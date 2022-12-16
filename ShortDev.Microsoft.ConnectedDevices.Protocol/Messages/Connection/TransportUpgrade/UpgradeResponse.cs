using ShortDev.Networking;
using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Messages.Connection.TransportUpgrade;

/// <summary>
/// This message transports the upgrade response.
/// </summary>
public sealed class UpgradeResponse : ICdpPayload<UpgradeResponse>
{
    /// <summary>
    /// A length-prefixed list of endpoint structures (see following) that are provided by each transport on the host device.
    /// </summary>
    public required HostEndpointMetadata[] HostEndpoints { get; init; }
    public required TransportEndpoint[] Endpoints { get; init; }

    public static UpgradeResponse Parse(BinaryReader reader)
    {
        throw new NotImplementedException();
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((ushort)HostEndpoints.Length);
        foreach (var endpoint in HostEndpoints)
        {
            writer.WriteWithLength(endpoint.Host);
            writer.Write((byte)0);
            writer.WriteWithLength(endpoint.Service);
            writer.Write((byte)0);
            writer.Write((ushort)endpoint.Type);
        }

        TransportEndpoint.WriteArray(writer, Endpoints);
    }
}
