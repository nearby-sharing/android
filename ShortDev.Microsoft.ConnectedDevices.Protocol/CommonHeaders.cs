using System;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol
{
    public sealed class CommonHeaders
    {
        public CommonHeaders() { }

        public bool TryRead(BinaryReader reader)
            => TryRead(reader, out _);
        public bool TryRead(BinaryReader reader, out Exception? ex)
        {
            var sig = reader.ReadUInt16();
            if (sig != Constants.Signature)
            {
                ex = new InvalidDataException($"Wrong signature. Expected \"{Constants.Signature}\"");
                return false;
            }

            MessageLength = reader.ReadUInt16();
            Version = reader.ReadByte();
            if (Version != Constants.ProtocolVersion)
            {
                ex = new InvalidDataException($"Wrong version. Got \"{Version}\", expected \"{Constants.ProtocolVersion}\"");
                return false;
            }

            Type = (MessageType)reader.ReadByte();
            Flags = (MessageFlags)reader.ReadInt16();
            SequenceNumber = reader.ReadUInt32();
            RequestID = reader.ReadUInt64();
            FragmentIndex = reader.ReadUInt16();
            FragmentCount = reader.ReadUInt16();
            SessionID = reader.ReadUInt64();
            ChannelID = reader.ReadUInt64();
            NextHeader = (NextHeaderType)reader.ReadByte();
            NextHeaderSize = reader.ReadByte();

            reader.ReadBytes(NextHeaderSize);

            if (Flags.HasFlag(MessageFlags.HasHMAC))
            {
                //reader.ReadBytes(64);
            }

            ex = null;
            return true;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Constants.Signature);
            writer.Write(MessageLength);
            writer.Write(Constants.ProtocolVersion);
            writer.Write((byte)Type);
            writer.Write((short)Flags);
            writer.Write(SequenceNumber);
            writer.Write(RequestID);
            writer.Write(FragmentIndex);
            writer.Write(FragmentCount);
            writer.Write(SessionID);
            writer.Write(ChannelID);
            writer.Write((byte)NextHeader);
            writer.Write(NextHeaderSize);
        }

        public uint MessageLength { get; set; }
        public byte Version { get; set; }
        public MessageType Type { get; set; }
        public MessageFlags Flags { get; set; }
        public uint SequenceNumber { get; set; }
        public ulong RequestID { get; set; }
        public ushort FragmentIndex { get; set; }
        public ushort FragmentCount { get; set; }
        public ulong SessionID { get; set; }
        public ulong ChannelID { get; set; }
        public NextHeaderType NextHeader { get; set; }
        public byte NextHeaderSize { get; set; }
    }
}
