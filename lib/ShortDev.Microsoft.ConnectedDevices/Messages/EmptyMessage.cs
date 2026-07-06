using System.Runtime.CompilerServices;

namespace ShortDev.Microsoft.ConnectedDevices.Messages;

internal readonly record struct EmptyMessage() : IBinaryParsable<EmptyMessage>, IBinaryWritable<EmptyMessage>
{
    public readonly void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        // No content to write
    }

    public static EmptyMessage Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
        => default;

    ulong IBinaryWritable<EmptyMessage>.MinimumSize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = 0;

    public static readonly EmptyMessage Instance = default;
}
