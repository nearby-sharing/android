namespace ShortDev.Microsoft.ConnectedDevices.Messages;

internal readonly record struct EmptyMessage : IBinaryParsable<EmptyMessage>, IBinaryWritable
{
    public readonly void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        // No content to write
    }

    public static EmptyMessage Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => default;
}
