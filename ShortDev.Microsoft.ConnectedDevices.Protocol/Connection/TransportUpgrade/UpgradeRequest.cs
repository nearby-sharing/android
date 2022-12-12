using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.TransportUpgrade;

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

    public static UpgradeRequest Parse(BinaryReader reader)
    {
        Guid id = new(reader.ReadBytes(16));

        var arrayLength = reader.ReadUInt16();
        var endpoints = new TransportEndpoint[arrayLength];
        for (int i = 0; i < arrayLength; i++)
        {
            var type = (EndpointType)reader.ReadUInt16();
            var length = reader.ReadInt32();
            var data = reader.ReadBytes(length);
            endpoints[i] = new(type, data);
        }
        return new()
        {
            UpgradeId = id,
            Endpoints = endpoints
        };
    }

    void ICdpWriteable.Write(BinaryWriter writer)
        => Write(writer, writeId: true);

    public void Write(BinaryWriter writer, bool writeId = true)
    {
        if (writeId)
            writer.Write(UpgradeId.ToByteArray());

        writer.Write((ushort)Endpoints.Length);
        foreach (var endpoint in Endpoints)
        {
            writer.Write((ushort)endpoint.Type);
            writer.Write((uint)endpoint.Data.Length);
            writer.Write(endpoint.Data);
        }
    }
}