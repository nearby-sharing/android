using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

public readonly record struct EndpointMetadata(CdpTransportType Type, byte[] Data) : IBinaryWritable, IBinaryParsable<EndpointMetadata>
{
    public static EndpointMetadata Tcp { get; } = new(CdpTransportType.Tcp, []);
    public static EndpointMetadata WifiDirect { get; } = new(CdpTransportType.WifiDirect, []);

    public static EndpointMetadata Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        var type = (CdpTransportType)reader.ReadUInt16();
        var length = reader.ReadInt32();

        byte[] data = new byte[length];
        reader.ReadBytes(data);
        return new(type, data);
    }

    public static IReadOnlyList<EndpointMetadata> ParseArray<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        var arrayLength = reader.ReadUInt16();
        var endpoints = new EndpointMetadata[arrayLength];
        for (int i = 0; i < arrayLength; i++)
            endpoints[i] = Parse(ref reader);
        return endpoints;
    }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write((ushort)Type);
        writer.Write((uint)Data.Length);
        writer.Write(Data);
    }

    public static void WriteArray<TWriter>(ref TWriter writer, IReadOnlyList<EndpointMetadata> endpoints) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write((ushort)endpoints.Count);
        foreach (var endpoint in endpoints)
            endpoint.Write(ref writer);
    }
}

public readonly record struct EndpointMetadataArray(IReadOnlyList<EndpointMetadata> Endpoints) : IBinaryWritable, IBinaryParsable<EndpointMetadataArray>
{
    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
        => EndpointMetadata.WriteArray(ref writer, Endpoints);
    public static EndpointMetadataArray Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new(EndpointMetadata.ParseArray(ref reader));
}
