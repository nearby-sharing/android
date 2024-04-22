using ShortDev.Microsoft.ConnectedDevices.Exceptions;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Control;

public sealed class StartChannelResponse : ICdpPayload<StartChannelResponse>
{
    public static StartChannelResponse Parse(ref EndianReader reader)
       => new()
       {
           Result = (ChannelResult)reader.ReadByte(),
           ChannelId = reader.ReadUInt64()
       };

    public required ChannelResult Result { get; init; }
    public required ulong ChannelId { get; init; }

    public void ThrowOnError()
    {
        if (Result != ChannelResult.Success)
            throw new CdpProtocolException($"Could not create channel. {Result}");
    }

    public void Write(EndianWriter writer)
    {
        writer.Write((byte)Result);
        writer.Write(ChannelId);
    }
}
