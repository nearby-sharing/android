using System;
using System.Collections.Generic;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol
{
    public sealed class CommonHeaders : ICdpHeader<CommonHeaders>
    {
        public static CommonHeaders Parse(BinaryReader reader)
            => throw new NotImplementedException();

        public static bool TryParse(BinaryReader reader, out CommonHeaders? result, out Exception? ex)
        {
            result = new();
            var sig = reader.ReadUInt16();
            if (sig != Constants.Signature)
            {
                ex = new InvalidDataException($"Wrong signature. Expected \"{Constants.Signature}\"");
                return false;
            }

            result.MessageLength = reader.ReadUInt16();
            result.Version = reader.ReadByte();
            if (result.Version != Constants.ProtocolVersion)
            {
                ex = new InvalidDataException($"Wrong version. Got \"{result.Version}\", expected \"{Constants.ProtocolVersion}\"");
                return false;
            }

            result.Type = (MessageType)reader.ReadByte();
            result.Flags = (MessageFlags)reader.ReadInt16();
            result.SequenceNumber = reader.ReadUInt32();
            result.RequestID = reader.ReadUInt64();
            result.FragmentIndex = reader.ReadUInt16();
            result.FragmentCount = reader.ReadUInt16();
            result.SessionID = reader.ReadUInt64();
            result.ChannelID = reader.ReadUInt64();

            NextHeaderType nextHeaderType;
            byte nextHeaderSize;
            List<AdditionalMessageHeader> additionalHeaders = new();
            while (true)
            {
                nextHeaderType = (NextHeaderType)reader.ReadByte();
                nextHeaderSize = reader.ReadByte();

                if (nextHeaderType != NextHeaderType.None)
                {
                    var value = reader.ReadBytes(nextHeaderSize);
                    additionalHeaders.Add(new(nextHeaderType, value));
                }
                else
                    break;
            }

            if (nextHeaderSize != 0)
            {
                ex = new InvalidDataException("Invalid header size, end-of-header cannot have a size greather than 0");
                return false;
            }

            result.AdditionalHeaders = additionalHeaders.ToArray();

            if (result.Flags.HasFlag(MessageFlags.HasHMAC))
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
        public byte Version { get; set; } = Constants.ProtocolVersion;
        public MessageType Type { get; set; }
        public MessageFlags Flags { get; set; }
        public uint SequenceNumber { get; set; } = 0;
        public ulong RequestID { get; set; } = 0;
        public ushort FragmentIndex { get; set; } = 0;
        public ushort FragmentCount { get; set; } = 1;
        public ulong SessionID { get; set; } = 0;
        public ulong ChannelID { get; set; } = 0;

        public AdditionalMessageHeader[] AdditionalHeaders { get; set; }
        public record AdditionalMessageHeader(NextHeaderType Type, byte[] Value);
    }
}
