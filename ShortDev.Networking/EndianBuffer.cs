using System;
using System.Buffers;

namespace ShortDev.Networking;

public sealed class EndianBuffer
{
    readonly ArrayBufferWriter<byte> _writer;

    public EndianBuffer()
        => _writer = new();

    public EndianBuffer(int initialCapacity)
        => _writer = new(initialCapacity);

    public void Write(byte value)
    {
        Span<byte> buffer = stackalloc byte[1];
        buffer[0] = value;
        _writer.Write(buffer);
    }

    public void Write(ReadOnlySpan<byte> buffer)
        => _writer.Write(buffer);

    public int Size
        => _writer.WrittenCount;

    public ReadOnlySpan<byte> AsSpan()
        => _writer.WrittenSpan;

    public void Clear()
        => _writer.Clear();
}
