using System;
using System.IO;

namespace ShortDev.Networking
{
    public sealed class BigEndianBinaryWriter : BinaryWriter
    {
        public BigEndianBinaryWriter(Stream stream) : base(stream) { }

        public override void Write(byte[] buffer)
            => Write(buffer, 0, buffer.Length);

        public override void Write(byte[] buffer, int index, int count)
        {
            // Array.Reverse(buffer);
            base.Write(buffer, index, count);
        }

        public override void Write(char[] chars)
            => Write(chars, 0, chars.Length);

        public override void Write(char[] chars, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(short value)
        {
            OutStream.WriteByte((byte)(value >> 8));
            OutStream.WriteByte((byte)value);
        }

        public override void Write(ushort value)
        {
            OutStream.WriteByte((byte)(value >> 8));
            OutStream.WriteByte((byte)value);
        }

        public override void Write(int value)
        {
            OutStream.WriteByte((byte)(value >> 24));
            OutStream.WriteByte((byte)(value >> 16));
            OutStream.WriteByte((byte)(value >> 8));
            OutStream.WriteByte((byte)value);
        }

        public override void Write(uint value)
        {
            OutStream.WriteByte((byte)(value >> 24));
            OutStream.WriteByte((byte)(value >> 16));
            OutStream.WriteByte((byte)(value >> 8));
            OutStream.WriteByte((byte)value);
        }

        public override void Write(long value)
        {
            OutStream.WriteByte((byte)(value >> 56));
            OutStream.WriteByte((byte)(value >> 48));
            OutStream.WriteByte((byte)(value >> 40));
            OutStream.WriteByte((byte)(value >> 32));
            OutStream.WriteByte((byte)(value >> 24));
            OutStream.WriteByte((byte)(value >> 16));
            OutStream.WriteByte((byte)(value >> 8));
            OutStream.WriteByte((byte)value);
        }

        public override void Write(ulong value)
        {
            OutStream.WriteByte((byte)(value >> 56));
            OutStream.WriteByte((byte)(value >> 48));
            OutStream.WriteByte((byte)(value >> 40));
            OutStream.WriteByte((byte)(value >> 32));
            OutStream.WriteByte((byte)(value >> 24));
            OutStream.WriteByte((byte)(value >> 16));
            OutStream.WriteByte((byte)(value >> 8));
            OutStream.WriteByte((byte)value);
        }


        //public override void Write(string value)
        //{
        //    throw new NotImplementedException();
        //}
        public override void Write(float value)
        {
            throw new NotImplementedException();
        }

        public override void Write(double value)
        {
            throw new NotImplementedException();
        }

        public override void Write(decimal value)
        {
            throw new NotImplementedException();
        }
    }
}
