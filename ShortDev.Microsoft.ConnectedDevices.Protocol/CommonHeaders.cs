using System;
using System.Collections.Generic;
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

            NextHeaderType nextHeaderType;
            byte nextHeaderSize;
            List<AdditionalMessageHeader> additionalHeaders = new();
            do
            {
                nextHeaderType = (NextHeaderType)reader.ReadByte();
                nextHeaderSize = reader.ReadByte();

                var value = reader.ReadBytes(nextHeaderSize);
                additionalHeaders.Add(new(nextHeaderType, value));
            } while (nextHeaderType != NextHeaderType.None);

            if (nextHeaderSize != 0)
            {
                ex = new InvalidDataException("Invalid header size, end-of-header cannot have a size greather than 0");
                return false;
            }

            AdditionalHeaders = additionalHeaders.ToArray();

            if (Flags.HasFlag(MessageFlags.HasHMAC))
            {
                // ToDo: HMAC ?!
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

            foreach (var header in AdditionalHeaders)
            {
                writer.Write((byte)header.Type);
                writer.Write(header.Value);
            }
            writer.Write((byte)NextHeaderType.None);
            writer.Write((byte)0);
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

        public AdditionalMessageHeader[] AdditionalHeaders { get; set; }
        public record AdditionalMessageHeader(NextHeaderType Type, byte[] Value);
    }
}
