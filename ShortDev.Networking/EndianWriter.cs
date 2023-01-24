using System;
using System.Buffers.Binary;

namespace ShortDev.Networking;

public sealed class EndianWriter
{
    public bool UseLittleEndian { get; init; } = false;

    public EndianBuffer Buffer { get; }
    public EndianWriter()
        => Buffer = new();
    public EndianWriter(EndianBuffer stream)
        => Buffer = stream;

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

    //public  void Write(string value)
    //{
    //    throw new NotImplementedException();
    //}

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

    //public void Write(decimal value)
    //{
    //    throw new NotImplementedException();
    //}
}
