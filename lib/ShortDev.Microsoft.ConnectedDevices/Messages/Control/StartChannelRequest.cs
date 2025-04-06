namespace ShortDev.Microsoft.ConnectedDevices.Messages.Control;

public readonly record struct StartChannelRequest() : IBinaryWritable, IBinaryParsable<StartChannelRequest>
{
    public static StartChannelRequest Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new()
        {
            Id = reader.ReadStringWithLength(),
            Name = reader.ReadStringWithLength(),
            Reserved1 = reader.ReadInt16(),
            Reserved2 = reader.ReadStringWithLength()
        };

    public required string Id { get; init; }
    public required string Name { get; init; }
    public short Reserved1 { get; init; }
    public string Reserved2 { get; init; } = string.Empty;

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.WriteWithLength(Id);
        writer.WriteWithLength(Name);
        writer.Write(Reserved1);
        writer.WriteWithLength(Reserved2);
    }
}
