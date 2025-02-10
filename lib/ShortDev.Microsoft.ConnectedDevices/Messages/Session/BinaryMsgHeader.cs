namespace ShortDev.Microsoft.ConnectedDevices.Messages.Session;

/// <summary>
/// cdp.dll!cdp::BinaryFragmenter::GetMessageFragments <br/>
/// <see cref="AdditionalHeaderType.UserMessageRequestId"/>
/// </summary>
public readonly struct BinaryMsgHeader() : IBinaryWritable, IBinaryParsable<BinaryMsgHeader>
{
    public uint FragmentCount { get; init; } = 1;
    public uint FragmentIndex { get; init; } = 0;
    public required uint MessageId { get; init; }

    public static BinaryMsgHeader Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => new()
        {
            FragmentCount = reader.ReadUInt32(),
            FragmentIndex = reader.ReadUInt32(),
            MessageId = reader.ReadUInt32(),
        };

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write(FragmentCount);
        writer.Write(FragmentIndex);
        writer.Write(MessageId);
    }
}
