using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

public record class EndpointMetadata(CdpTransportType Type, byte[] Data) : ICdpPayload<EndpointMetadata>, ICdpArraySerializable<EndpointMetadata>
{
    public static EndpointMetadata Tcp { get; } = new(CdpTransportType.Tcp, []);
    public static EndpointMetadata WifiDirect { get; } = new(CdpTransportType.WifiDirect, []);

    public static EndpointMetadata Parse(ref EndianReader reader)
    {
        var type = (CdpTransportType)reader.ReadUInt16();
        var length = reader.ReadInt32();
        var data = reader.ReadBytes(length);
        return new(type, data.ToArray());
    }

    public static IReadOnlyList<EndpointMetadata> ParseArray(ref EndianReader reader)
    {
        var arrayLength = reader.ReadUInt16();
        var endpoints = new EndpointMetadata[arrayLength];
        for (int i = 0; i < arrayLength; i++)
            endpoints[i] = Parse(ref reader);
        return endpoints;
    }

    public void Write(EndianWriter writer)
    {
        writer.Write((ushort)Type);
        writer.Write((uint)Data.Length);
        writer.Write(Data);
    }

    public static void WriteArray(EndianWriter writer, IReadOnlyList<EndpointMetadata> endpoints)
    {
        writer.Write((ushort)endpoints.Count);
        foreach (var endpoint in endpoints)
            endpoint.Write(writer);
    }
}
