﻿using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace ShortDev.Networking;

public readonly struct EndianBuffer : IBufferWriter<byte>
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

    [SuppressMessage("Style", "IDE0302:Simplify collection initialization", Justification = "Seems like this allocates a new array instead of using stackalloc!")]
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

    void IBufferWriter<byte>.Advance(int count) => _writer.Advance(count);
    Memory<byte> IBufferWriter<byte>.GetMemory(int sizeHint) => _writer.GetMemory(sizeHint);
    Span<byte> IBufferWriter<byte>.GetSpan(int sizeHint) => _writer.GetSpan(sizeHint);
}
