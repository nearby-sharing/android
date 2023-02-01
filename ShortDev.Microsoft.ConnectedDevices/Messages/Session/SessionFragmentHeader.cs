using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Session;

/// <summary>
/// cdp.dll!cdp::BinaryFragmenter::GetMessageFragments <br/>
/// <see cref="AdditionalHeaderType.UserMessageRequestId"/>
/// </summary>
public sealed class SessionFragmentHeader : ICdpHeader<SessionFragmentHeader>
{
    public int FragmentCount { get; set; } = 1;
    public int FragmentIndex { get; set; } = 0;
    public required int MessageId { get; set; }

    public static SessionFragmentHeader Parse(BinaryReader reader)
        => new()
        {
            FragmentCount = reader.ReadInt32(),
            FragmentIndex = reader.ReadInt32(),
            MessageId = reader.ReadInt32(),
        };

    public void Write(BinaryWriter writer)
    {
        writer.Write(FragmentCount);
        writer.Write(FragmentIndex);
        writer.Write(MessageId);
    }
}
