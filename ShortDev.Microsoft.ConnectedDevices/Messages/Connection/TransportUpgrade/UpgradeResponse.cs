using ShortDev.Microsoft.ConnectedDevices.Transports;
using ShortDev.Networking;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

/// <summary>
/// This message transports the upgrade response.
/// </summary>
public sealed class UpgradeResponse : ICdpPayload<UpgradeResponse>
{
    /// <summary>
    /// A length-prefixed list of endpoint structures (see following) that are provided by each transport on the host device.
    /// </summary>
    public required EndpointInfo[] Endpoints { get; init; }
    public required EndpointMetadata[] MetaData { get; init; }

    public static UpgradeResponse Parse(ref EndianReader reader)
    {
        var length = reader.ReadUInt16();
        var endpoints = new EndpointInfo[length];
        for (int i = 0; i < length; i++)
        {
            var host = reader.ReadStringWithLength();
            var service = reader.ReadStringWithLength();
            var type = (CdpTransportType)reader.ReadUInt16();

            endpoints[i] = new(type, host, service);
        }

        var metaData = EndpointMetadata.ParseArray(ref reader);

        return new()
        {
            Endpoints = endpoints,
            MetaData = metaData
        };
    }

    public void Write(EndianWriter writer)
    {
        writer.Write((ushort)Endpoints.Length);
        foreach (var endpoint in Endpoints)
        {
            writer.WriteWithLength(endpoint.Address);
            writer.WriteWithLength(endpoint.Service);
            writer.Write((ushort)endpoint.TransportType);
        }

        EndpointMetadata.WriteArray(writer, MetaData);
    }
}
