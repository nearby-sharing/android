using System;
using System.Buffers;

namespace ShortDev.Networking;

public readonly struct EndianBuffer
{
    readonly ArrayBufferWriter<byte> _writer;

    public EndianBuffer()
        => _writer = new();

    public EndianBuffer(int initialCapacity)
        => _writer = new(initialCapacity);

    public EndianBuffer(byte[] data)
    {
        _writer = new(data.Length);
        _writer.Write(data);
    }

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

    public Span<byte> AsWriteableSpan()
        => _writer.WrittenSpan.AsSpan();

    public ReadOnlyMemory<byte> AsMemory()
        => _writer.WrittenMemory;

    public byte[] ToArray()
        => _writer.WrittenMemory.ToArray();

    public void Clear()
        => _writer.Clear();
}
