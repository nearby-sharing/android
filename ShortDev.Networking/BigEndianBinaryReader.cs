using System;
using System.IO;

namespace ShortDev.Networking
{
    public sealed class BigEndianBinaryReader : BinaryReader
    {
        public BigEndianBinaryReader(Stream stream) : base(stream) { }

        public override short ReadInt16()
            => (short)(ReadByte() << 8 | ReadByte());

        public override ushort ReadUInt16()
            => (ushort)(ReadByte() << 8 | ReadByte());

        public override int ReadInt32()
            => (int)(ReadByte() << 24 | ReadByte() << 16 | ReadByte() << 8 | ReadByte());

        public override uint ReadUInt32()
            => (uint)(ReadByte() << 24 | ReadByte() << 16 | ReadByte() << 8 | ReadByte());

        public override long ReadInt64()
        {
            uint hi = (uint)(ReadByte() << 24 | ReadByte() << 16 | ReadByte() << 8 | ReadByte());
            uint lo = (uint)(ReadByte() << 24 | ReadByte() << 16 | ReadByte() << 8 | ReadByte());
            return (long)(((ulong)hi) << 32 | lo);
        }

        public override ulong ReadUInt64()
        {
            uint hi = (uint)(ReadByte() << 24 | ReadByte() << 16 | ReadByte() << 8 | ReadByte());
            uint lo = (uint)(ReadByte() << 24 | ReadByte() << 16 | ReadByte() << 8 | ReadByte());
            return ((ulong)hi) << 32 | lo;
        }

        public override string ReadString()
        {
            throw new NotImplementedException();
        }

        public override float ReadSingle()
        {
            throw new NotImplementedException();
        }

        public override double ReadDouble()
        {
            throw new NotImplementedException();
        }

        public override decimal ReadDecimal()
        {
            throw new NotImplementedException();
        }
    }
}
