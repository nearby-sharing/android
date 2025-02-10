namespace ShortDev.Microsoft.ConnectedDevices.Messages.Discovery;

public sealed class DiscoveryHeader : IBinaryWritable, IBinaryParsable<DiscoveryHeader>
{
    public static DiscoveryHeader Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new()
        {
            Type = (DiscoveryType)reader.ReadUInt8()
        };

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write((byte)Type);
    }

    public required DiscoveryType Type { get; set; }
}
