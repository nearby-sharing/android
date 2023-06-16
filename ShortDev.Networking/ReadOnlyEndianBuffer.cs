using System;
using System.Diagnostics;
using System.IO;

namespace ShortDev.Networking;

public readonly ref struct ReadOnlyEndianBuffer
{
    readonly Indexer _indexer = new();

    readonly ReadOnlySpan<byte> _buffer;
    readonly Stream? _stream;

    public ReadOnlyEndianBuffer(ReadOnlySpan<byte> buffer)
        => _buffer = buffer;

    public ReadOnlyEndianBuffer(Stream stream)
        => _stream = stream;

    public void ReadBytes(Span<byte> buffer)
    {
        if (_stream != null)
        {
            ReadStreamInternal(buffer);
            return;
        }

        _buffer.Slice(_indexer.index, buffer.Length).CopyTo(buffer);
        _indexer.index += buffer.Length;
    }

    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        if (_stream != null)
        {
            Span<byte> buffer = new byte[length];
            _stream.Read(buffer);
            return buffer;
        }

        var slice = _buffer.Slice(_indexer.index, length);
        _indexer.index += length;
        return slice;
    }

    public byte ReadByte()
    {
        if (_stream != null)
        {
            var buffer = _stream.ReadByte();
            if (buffer == -1)
                throw new EndOfStreamException();

            return (byte)buffer;
        }

        var value = _buffer[_indexer.index];
        _indexer.index++;
        return value;
    }

    public readonly ReadOnlySpan<byte> AsSpan()
    {
        if (_stream != null)
            throw new InvalidOperationException("Not supported by stream");

        return _buffer;
    }

    public ReadOnlySpan<byte> ReadToEnd()
    {
        if (_stream != null)
            throw new InvalidOperationException("Not supported by stream");

        return ReadBytes(_buffer.Length - _indexer.index);
    }

    /// <summary>
    /// <see href="https://github.com/dotnet/runtime/blob/56c84971041ae1debfa5ff360c547392d29f4cb3/src/libraries/System.Private.CoreLib/src/System/IO/BinaryReader.cs#L494-L506">BinaryReader.ReadBytes</see>
    /// </summary>
    readonly void ReadStreamInternal(Span<byte> buffer)
    {
        Debug.Assert(_stream != null);

        int count = buffer.Length;
        int numRead = 0;
        do
        {
            int n = _stream.Read(buffer[numRead..]);
            if (n == 0)
                throw new EndOfStreamException();

            numRead += n;
            count -= n;
        } while (count > 0);

        Debug.Assert(numRead == buffer.Length);
    }

    // ToDo: Remove allocation
    sealed class Indexer
    {
        public int index;
    }
}
