using Microsoft.CorrelationVector;
using System.Buffers.Binary;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.Messages;

/// <summary>
/// 
/// (See <see cref="CommonHeader.AdditionalHeaders"/>)
/// </summary>
/// <param name="Type"></param>
/// <param name="Value"></param>
public readonly record struct AdditionalHeader(AdditionalHeaderType Type, ReadOnlyMemory<byte> Value)
{
    public static AdditionalHeader CreateCorrelationHeader()
        => FromCorrelationVector(new CorrelationVectorV1());

    public static AdditionalHeader FromCorrelationVector(CorrelationVector cv)
        => FromCorrelationVector(cv.Value);

    public static AdditionalHeader FromCorrelationVector(string cv)
    {
        return new(
            AdditionalHeaderType.CorrelationVector,
            Encoding.ASCII.GetBytes(cv)
        );
    }

    public static AdditionalHeader FromUInt64(AdditionalHeaderType type, ulong value)
    {
        Memory<byte> buffer = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(buffer.Span, value);
        return new(type, buffer);
    }

    public static AdditionalHeader FromUInt32(AdditionalHeaderType type, uint value)
    {
        Memory<byte> buffer = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Span, value);
        return new(type, buffer);
    }

    public ulong AsUInt64()
        => BinaryPrimitives.ReadUInt64BigEndian(Value.Span);

    public ulong AsUInt32()
        => BinaryPrimitives.ReadUInt32BigEndian(Value.Span);

    public CorrelationVector ToCorrelationVector()
    {
        var raw = Encoding.ASCII.GetString(Value.Span);
        return CorrelationVector.Parse(raw);
    }
}
