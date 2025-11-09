using ShortDev.Microsoft.ConnectedDevices.Exceptions;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Control;

public readonly record struct StartChannelResponse : IBinaryWritable, IBinaryParsable<StartChannelResponse>
{
    public static StartChannelResponse Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
       => new()
       {
           Result = (ChannelResult)reader.ReadUInt8(),
           ChannelId = reader.ReadUInt64()
       };

    public required ChannelResult Result { get; init; }
    public required ulong ChannelId { get; init; }

    public void ThrowOnError()
    {
        if (Result != ChannelResult.Success)
            throw new CdpProtocolException($"Could not create channel. {Result}");
    }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write((byte)Result);
        writer.Write(ChannelId);
    }
}
