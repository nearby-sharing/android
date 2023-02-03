using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace ShortDev.Networking;

public readonly ref struct EndianWriter
{
    static readonly Encoding DefaultEncoding = Encoding.UTF8;

    public readonly bool UseLittleEndian;
    public readonly EndianBuffer Buffer;

    public EndianWriter() : this(Endianness.BigEndian) { }

    public EndianWriter(Endianness endianness)
    {
        UseLittleEndian = endianness == Endianness.LittleEndian;
        Buffer = new();
    }

    public EndianWriter(Endianness endianness, int initialCapacity)
    {
        UseLittleEndian = endianness == Endianness.LittleEndian;
        Buffer = new(initialCapacity);
    }

    public void Clear()
        => Buffer.Clear();


    public void Write(ReadOnlySpan<byte> buffer)
        => Buffer.Write(buffer);

    public void Write(EndianBuffer buffer)
        => Buffer.Write(buffer.AsSpan());

    public void Write(byte value)
        => Buffer.Write(value);

    public void Write(short value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(short)];

        if (UseLittleEndian)
            BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
        else
            BinaryPrimitives.WriteInt16BigEndian(buffer, value);

        Write(buffer);
    }

    public void Write(ushort value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];

        if (UseLittleEndian)
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        else
            BinaryPrimitives.WriteUInt16BigEndian(buffer, value);

        Write(buffer);
    }

    public void Write(int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];

        if (UseLittleEndian)
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        else
            BinaryPrimitives.WriteInt32BigEndian(buffer, value);

        Write(buffer);
    }

    public void Write(uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];

        if (UseLittleEndian)
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        else
            BinaryPrimitives.WriteUInt32BigEndian(buffer, value);

        Write(buffer);
    }

    public void Write(long value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];

        if (UseLittleEndian)
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
        else
            BinaryPrimitives.WriteInt64BigEndian(buffer, value);

        Write(buffer);
    }

    public void Write(ulong value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];

        if (UseLittleEndian)
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        else
            BinaryPrimitives.WriteUInt64BigEndian(buffer, value);

        Write(buffer);
    }

    public void Write(float value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(float)];

        if (UseLittleEndian)
            BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
        else
            BinaryPrimitives.WriteSingleBigEndian(buffer, value);

        Write(buffer);
    }

    public void Write(double value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(double)];

        if (UseLittleEndian)
            BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
        else
            BinaryPrimitives.WriteDoubleBigEndian(buffer, value);

        Write(buffer);
    }

    public void Write(string value)
        => Write(value, DefaultEncoding);

    public void Write(string value, Encoding encoding)
    {
        // ToDo: Allocation free if possible (no stack overflow)
        Write(encoding.GetBytes(value + "\0"));
    }

    public void WriteWithLength(string value)
        => WriteWithLength(value, DefaultEncoding);

    public void WriteWithLength(string value, Encoding encoding)
    {
        // ToDo: Allocation free if possible (no stack overflow)
        WriteWithLength(encoding.GetBytes(value + "\0"));
    }

    public void WriteWithLength(ReadOnlySpan<byte> value)
    {
        Write((ushort)value.Length);
        Write(value);
    }

    public void CopyTo(BinaryWriter writer)
    {
        writer.Write(Buffer.AsSpan());
        writer.Flush();
    }
}
