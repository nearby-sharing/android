using System;

namespace ShortDev.Networking;

public readonly ref struct ReadOnlyEndianBuffer
{
    readonly Indexer _indexer = new();

    readonly ReadOnlySpan<byte> _buffer;
    public ReadOnlyEndianBuffer(ReadOnlySpan<byte> buffer)
        => _buffer = buffer;

    public void ReadBytes(Span<byte> buffer)
    {
        _buffer.Slice(_indexer.index, buffer.Length).CopyTo(buffer);
        _indexer.index += buffer.Length;
    }

    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        var buffer = _buffer.Slice(_indexer.index, length);
        _indexer.index += length;
        return buffer;
    }

    public byte ReadByte()
    {
        var value = _buffer[_indexer.index];
        _indexer.index++;
        return value;
    }

    public ReadOnlySpan<byte> AsSpan()
        => _buffer;

    public ReadOnlySpan<byte> ReadToEnd()
        => ReadBytes(_buffer.Length - _indexer.index);

    class Indexer
    {
        public int index;
    }
}
