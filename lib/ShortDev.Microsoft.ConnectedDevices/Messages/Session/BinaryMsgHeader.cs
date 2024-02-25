namespace ShortDev.Microsoft.ConnectedDevices.Messages.Session;

/// <summary>
/// cdp.dll!cdp::BinaryFragmenter::GetMessageFragments <br/>
/// <see cref="AdditionalHeaderType.UserMessageRequestId"/>
/// </summary>
public sealed class BinaryMsgHeader : ICdpHeader<BinaryMsgHeader>
{
    public uint FragmentCount { get; set; } = 1;
    public uint FragmentIndex { get; set; } = 0;
    public required uint MessageId { get; set; }

    public static BinaryMsgHeader Parse(ref EndianReader reader)
        => new()
        {
            FragmentCount = reader.ReadUInt32(),
            FragmentIndex = reader.ReadUInt32(),
            MessageId = reader.ReadUInt32(),
        };

    public void Write(EndianWriter writer)
    {
        writer.Write(FragmentCount);
        writer.Write(FragmentIndex);
        writer.Write(MessageId);
    }
}
