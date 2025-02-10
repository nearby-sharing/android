using System.Runtime.CompilerServices;

namespace ShortDev.Microsoft.ConnectedDevices;

[InlineArray(32)]
public struct DeviceIdHash : IBinaryWritable, IBinaryParsable<DeviceIdHash>
{
    public byte _element0;

    public readonly void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
        => writer.Write((ReadOnlySpan<byte>)this);

    public static DeviceIdHash Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        DeviceIdHash result = default;
        reader.ReadBytes(result);
        return result;
    }
}
