namespace ShortDev.Microsoft.ConnectedDevices.AppControl.Messages;

public readonly record struct AppControlHeader : IBinaryWritable<AppControlHeader>, IBinaryParsable<AppControlHeader>
{
    public required AppControlType MessageType { get; init; }

    public static AppControlHeader Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        return new()
        {
            MessageType = (AppControlType)reader.ReadUInt8()
        };
    }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write((byte)MessageType);
    }
}
