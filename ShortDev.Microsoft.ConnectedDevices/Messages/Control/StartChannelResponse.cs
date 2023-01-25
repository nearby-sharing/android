using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Control;

public sealed class StartChannelResponse : ICdpPayload<StartChannelResponse>
{
    public static StartChannelResponse Parse(BinaryReader reader)
       => new()
       {
           Result = (ChannelResult)reader.ReadByte(),
           ChannelId = reader.ReadUInt64()
       };

    public required ChannelResult Result { get; init; }
    public required ulong ChannelId { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)Result);
        writer.Write(ChannelId);
    }
}
