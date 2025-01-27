using Bond;
using System.Runtime.CompilerServices;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.Serialization;
public readonly ref struct CompactBinaryBondWriter(HeapOutputBuffer buffer)
{
    const ushort Magic = 16963;

    readonly ushort version = 1;
    readonly EndianWriter output = new(Endianness.LittleEndian, buffer);

    //
    // Summary:
    //     Write protocol magic number and version
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVersion()
    {
        output.Write((ushort)16963);
        output.Write((ushort)version);
    }

    //
    // Summary:
    //     End writing a struct
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStructEnd()
    {
        output.Write((byte)0);
    }

    //
    // Summary:
    //     End writing a base struct
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBaseEnd()
    {
        output.Write((byte)1);
    }

    //
    // Summary:
    //     Start writing a field
    //
    // Parameters:
    //   type:
    //     Type of the field
    //
    //   id:
    //     Identifier of the field
    //
    //   metadata:
    //     Metadata of the field
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFieldBegin(BondDataType type, ushort id)
    {
        if (id <= 5)
        {
            output.Write((byte)((uint)type | (uint)(id << 5)));
            return;
        }

        if (id <= 255)
        {
            output.Write((ushort)((uint)type | (uint)(id << 8) | 0xC0u));
            return;
        }

        output.Write((byte)(type | (BondDataType)224));
        output.Write(id);
    }

    //
    // Summary:
    //     Start writing a list or set container
    //
    // Parameters:
    //   count:
    //     Number of elements in the container
    //
    //   elementType:
    //     Type of the elements
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteContainerBegin(int count, BondDataType elementType)
    {
        if (2 == version && count < 7)
        {
            output.Write((byte)((uint)elementType | (uint)(count + 1 << 5)));
            return;
        }

        output.Write((byte)elementType);
        IntegerHelper.WriteVarUInt32(output, (uint)count);
    }

    //
    // Summary:
    //     Start writing a map container
    //
    // Parameters:
    //   count:
    //     Number of elements in the container
    //
    //   keyType:
    //     Type of the keys
    //
    //   valueType:
    //     Type of the values
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteContainerBegin(int count, BondDataType keyType, BondDataType valueType)
    {
        output.Write((byte)keyType);
        output.Write((byte)valueType);
        IntegerHelper.WriteVarUInt32(output, (uint)count);
    }

    //
    // Summary:
    //     Write array of bytes verbatim
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        output.Write(data);
    }

    //
    // Summary:
    //     Write an UInt8
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt8(byte value)
    {
        output.Write(value);
    }

    //
    // Summary:
    //     Write an UInt16
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt16(ushort value)
    {
        IntegerHelper.WriteVarUInt16(output, value);
    }

    //
    // Summary:
    //     Write an UInt16
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt32(uint value)
    {
        IntegerHelper.WriteVarUInt32(output, value);
    }

    //
    // Summary:
    //     Write an UInt64
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt64(ulong value)
    {
        IntegerHelper.WriteVarUInt64(output, value);
    }

    //
    // Summary:
    //     Write an Int8
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt8(sbyte value)
    {
        output.Write((byte)value);
    }

    //
    // Summary:
    //     Write an Int16
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt16(short value)
    {
        IntegerHelper.WriteVarUInt16(output, IntegerHelper.EncodeZigzag16(value));
    }

    //
    // Summary:
    //     Write an Int32
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt32(int value)
    {
        IntegerHelper.WriteVarUInt32(output, IntegerHelper.EncodeZigzag32(value));
    }

    //
    // Summary:
    //     Write an Int64
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt64(long value)
    {
        IntegerHelper.WriteVarUInt64(output, IntegerHelper.EncodeZigzag64(value));
    }

    //
    // Summary:
    //     Write a float
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat(float value)
    {
        output.Write(value);
    }

    //
    // Summary:
    //     Write a double
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDouble(double value)
    {
        output.Write(value);
    }

    //
    // Summary:
    //     Write a bool
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value)
    {
        output.Write((byte)(value ? 1u : 0u));
    }

    //
    // Summary:
    //     Write a UTF-8 string
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(string value)
    {
        if (value.Length == 0)
        {
            WriteUInt32(0u);
            return;
        }

        int byteCount = Encoding.UTF8.GetByteCount(value);
        WriteUInt32((uint)byteCount);
        // output.WriteString(Encoding.UTF8, value, byteCount);
        output.Write(Encoding.UTF8.GetBytes(value));
    }

    //
    // Summary:
    //     Write a UTF-16 string
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteWString(string value)
    {
        if (value.Length == 0)
        {
            WriteUInt32(0u);
            return;
        }

        int size = checked(value.Length * 2);
        WriteUInt32((uint)value.Length);
        // output.WriteString(Encoding.Unicode, value, size);
        output.Write(Encoding.Unicode.GetBytes(value));
    }

    internal static class IntegerHelper
    {
        public const int MaxBytesVarInt16 = 3;

        public const int MaxBytesVarInt32 = 5;

        public const int MaxBytesVarInt64 = 10;

        public static int GetVarUInt16Length(ushort value)
        {
            if (value < 128)
            {
                return 1;
            }

            if (value < 16384)
            {
                return 2;
            }

            return 3;
        }

        public static void WriteVarUInt16(in EndianWriter writer, ushort value)
        {
            if (value >= 128)
            {
                writer.Write((byte)(value | 0x80u));
                value >>= 7;
                if (value >= 128)
                {
                    writer.Write((byte)(value | 0x80u));
                    value >>= 7;
                }
            }

            writer.Write((byte)value);
        }

        public static int GetVarUInt32Length(uint value)
        {
            if (value < 128)
            {
                return 1;
            }

            if (value < 16384)
            {
                return 2;
            }

            if (value < 2097152)
            {
                return 3;
            }

            if (value < 268435456)
            {
                return 4;
            }

            return 5;
        }

        public static void WriteVarUInt32(in EndianWriter writer, uint value)
        {
            if (value >= 128)
            {
                writer.Write((byte)(value | 0x80u));
                value >>= 7;
                if (value >= 128)
                {
                    writer.Write((byte)(value | 0x80u));
                    value >>= 7;
                    if (value >= 128)
                    {
                        writer.Write((byte)(value | 0x80u));
                        value >>= 7;
                        if (value >= 128)
                        {
                            writer.Write((byte)(value | 0x80u));
                            value >>= 7;
                        }
                    }
                }
            }

            writer.Write((byte)value);
        }

        public static int GetVarUInt64Length(ulong value)
        {
            if (value < 128)
            {
                return 1;
            }

            if (value < 16384)
            {
                return 2;
            }

            if (value < 2097152)
            {
                return 3;
            }

            if (value < 268435456)
            {
                return 4;
            }

            if (value < 34359738368L)
            {
                return 5;
            }

            if (value < 4398046511104L)
            {
                return 6;
            }

            if (value < 562949953421312L)
            {
                return 7;
            }

            if (value < 72057594037927936L)
            {
                return 8;
            }

            if (value < 9223372036854775808uL)
            {
                return 9;
            }

            return 10;
        }

        public static void WriteVarUInt64(in EndianWriter writer, ulong value)
        {
            if (value >= 128)
            {
                writer.Write((byte)(value | 0x80));
                value >>= 7;
                if (value >= 128)
                {
                    writer.Write((byte)(value | 0x80));
                    value >>= 7;
                    if (value >= 128)
                    {
                        writer.Write((byte)(value | 0x80));
                        value >>= 7;
                        if (value >= 128)
                        {
                            writer.Write((byte)(value | 0x80));
                            value >>= 7;
                            if (value >= 128)
                            {
                                writer.Write((byte)(value | 0x80));
                                value >>= 7;
                                if (value >= 128)
                                {
                                    writer.Write((byte)(value | 0x80));
                                    value >>= 7;
                                    if (value >= 128)
                                    {
                                        writer.Write((byte)(value | 0x80));
                                        value >>= 7;
                                        if (value >= 128)
                                        {
                                            writer.Write((byte)(value | 0x80));
                                            value >>= 7;
                                            if (value >= 128)
                                            {
                                                writer.Write((byte)(value | 0x80));
                                                value >>= 7;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            writer.Write((byte)value);
        }

        public static ushort DecodeVarUInt16(byte[] data, ref int index)
        {
            int num = index;
            uint num2 = data[num++];
            if (128 <= num2)
            {
                uint num3 = data[num++];
                num2 = (num2 & 0x7Fu) | ((num3 & 0x7F) << 7);
                if (128 <= num3)
                {
                    num3 = data[num++];
                    num2 |= num3 << 14;
                }
            }

            index = num;
            return (ushort)num2;
        }

        public static uint DecodeVarUInt32(byte[] data, ref int index)
        {
            int num = index;
            uint num2 = data[num++];
            if (128 <= num2)
            {
                uint num3 = data[num++];
                num2 = (num2 & 0x7Fu) | ((num3 & 0x7F) << 7);
                if (128 <= num3)
                {
                    num3 = data[num++];
                    num2 |= (num3 & 0x7F) << 14;
                    if (128 <= num3)
                    {
                        num3 = data[num++];
                        num2 |= (num3 & 0x7F) << 21;
                        if (128 <= num3)
                        {
                            num3 = data[num++];
                            num2 |= num3 << 28;
                        }
                    }
                }
            }

            index = num;
            return num2;
        }

        public static ulong DecodeVarUInt64(byte[] data, ref int index)
        {
            int num = index;
            ulong num2 = data[num++];
            if (128 <= num2)
            {
                ulong num3 = data[num++];
                num2 = (num2 & 0x7F) | ((num3 & 0x7F) << 7);
                if (128 <= num3)
                {
                    num3 = data[num++];
                    num2 |= (num3 & 0x7F) << 14;
                    if (128 <= num3)
                    {
                        num3 = data[num++];
                        num2 |= (num3 & 0x7F) << 21;
                        if (128 <= num3)
                        {
                            num3 = data[num++];
                            num2 |= (num3 & 0x7F) << 28;
                            if (128 <= num3)
                            {
                                num3 = data[num++];
                                num2 |= (num3 & 0x7F) << 35;
                                if (128 <= num3)
                                {
                                    num3 = data[num++];
                                    num2 |= (num3 & 0x7F) << 42;
                                    if (128 <= num3)
                                    {
                                        num3 = data[num++];
                                        num2 |= (num3 & 0x7F) << 49;
                                        if (128 <= num3)
                                        {
                                            num3 = data[num++];
                                            num2 |= num3 << 56;
                                            if (128 <= num3)
                                            {
                                                num++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            index = num;
            return num2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort EncodeZigzag16(short value)
        {
            return (ushort)((value << 1) ^ (value >> 15));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint EncodeZigzag32(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong EncodeZigzag64(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short DecodeZigzag16(ushort value)
        {
            return (short)((value >> 1) ^ -(value & 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DecodeZigzag32(uint value)
        {
            return (int)((value >> 1) ^ (0L - (long)(value & 1)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long DecodeZigzag64(ulong value)
        {
            return (long)((value >> 1) ^ (0L - (value & 1)));
        }
    }
}

