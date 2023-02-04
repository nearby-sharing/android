using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ShortDev.Networking;

public readonly ref struct EndianReader
{
    /// <summary>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/stackalloc"><c>stackalloc</c></see>
    /// </summary>
    const int MaxStackLimit = 1024;
    static readonly Encoding DefaultEncoding = Encoding.UTF8;

    public readonly bool UseLittleEndian;
    public readonly ReadOnlyEndianBuffer Buffer;
    public readonly Stream? Stream;

    EndianReader(Endianness endianness)
    {
        UseLittleEndian = endianness == Endianness.LittleEndian;
    }

    public EndianReader(Endianness endianness, ReadOnlySpan<byte> data) : this(endianness)
        => Buffer = new(data);

    public EndianReader(Endianness endianness, Stream stream) : this(endianness)
        => Stream = stream;

    public ReadOnlySpan<byte> ReadToEnd()
        => Buffer.ReadToEnd();

    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        if (Stream != null)
        {
            var buffer = new byte[length];
            ReadStreamInternal(buffer);
            return buffer;
        }

        return Buffer.ReadBytes(length);
    }

    public void ReadBytes(Span<byte> buffer)
    {
        if (Stream != null)
            ReadStreamInternal(buffer);
        else
            Buffer.ReadBytes(buffer);
    }

    /// <summary>
    /// <see href="https://github.com/dotnet/runtime/blob/56c84971041ae1debfa5ff360c547392d29f4cb3/src/libraries/System.Private.CoreLib/src/System/IO/BinaryReader.cs#L494-L506">BinaryReader.ReadBytes</see>
    /// </summary>
    void ReadStreamInternal(Span<byte> buffer)
    {
        Debug.Assert(Stream != null);

        int count = buffer.Length;
        int numRead = 0;
        do
        {
            int n = Stream.Read(buffer[numRead..]);
            if (n == 0)
                throw new EndOfStreamException();

            numRead += n;
            count -= n;
        } while (count > 0);
        
        Debug.Assert(numRead == buffer.Length);
    }

    public byte ReadByte()
    {
        if (Stream != null)
        {
            var buffer = Stream.ReadByte();
            if (buffer == -1)
                throw new EndOfStreamException();
            return (byte)buffer;
        }

        return Buffer.ReadByte();
    }

    public short ReadInt16()
    {
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        ReadBytes(buffer);

        if (UseLittleEndian)
            return BinaryPrimitives.ReadInt16LittleEndian(buffer);
        else
            return BinaryPrimitives.ReadInt16BigEndian(buffer);
    }

    public ushort ReadUInt16()
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        ReadBytes(buffer);

        if (UseLittleEndian)
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        else
            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
    }

    public int ReadInt32()
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        ReadBytes(buffer);

        if (UseLittleEndian)
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        else
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
    }

    public uint ReadUInt32()
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        ReadBytes(buffer);

        if (UseLittleEndian)
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        else
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
    }

    public long ReadInt64()
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        ReadBytes(buffer);

        if (UseLittleEndian)
            return BinaryPrimitives.ReadInt64LittleEndian(buffer);
        else
            return BinaryPrimitives.ReadInt64BigEndian(buffer);
    }

    public ulong ReadUInt64()
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        ReadBytes(buffer);

        if (UseLittleEndian)
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        else
            return BinaryPrimitives.ReadUInt64BigEndian(buffer);
    }

    public float ReadSingle()
    {
        Span<byte> buffer = stackalloc byte[sizeof(float)];
        ReadBytes(buffer);

        if (UseLittleEndian)
            return BinaryPrimitives.ReadSingleLittleEndian(buffer);
        else
            return BinaryPrimitives.ReadSingleBigEndian(buffer);
    }

    public double ReadDouble()
    {
        Span<byte> buffer = stackalloc byte[sizeof(double)];
        ReadBytes(buffer);

        if (UseLittleEndian)
            return BinaryPrimitives.ReadDoubleLittleEndian(buffer);
        else
            return BinaryPrimitives.ReadDoubleBigEndian(buffer);
    }

    public string ReadStringWithLength()
        => ReadStringWithLength(DefaultEncoding);

    public string ReadStringWithLength(Encoding encoding)
    {
        var result = encoding.GetString(ReadBytesWithLength());
        ReadByte(); // Zero byte
        return result;
    }

    public ReadOnlySpan<byte> ReadBytesWithLength()
    {
        var length = ReadUInt16();
        return ReadBytes(length);
    }
}
