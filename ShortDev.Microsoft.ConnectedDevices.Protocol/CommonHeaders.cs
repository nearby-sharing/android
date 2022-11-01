using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol
{
    public sealed class CommonHeader : ICdpHeader<CommonHeader>
    {
        public static CommonHeader Parse(BinaryReader reader)
        {
            if (!TryParse(reader, out var result, out var ex))
                throw ex ?? new NullReferenceException("No exception");
            return result ?? throw new NullReferenceException("No result");
        }

        public static bool TryParse(BinaryReader reader, out CommonHeader? result, out Exception? ex)
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

            result.AdditionalHeaders = additionalHeaders;

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
                writer.Write((byte)header.Value.Length);
                writer.Write(header.Value);
            }
            writer.Write((byte)NextHeaderType.None);
            writer.Write((byte)0);
        }

        public ushort MessageLength { get; set; }
        public byte Version { get; set; } = Constants.ProtocolVersion;
        public MessageType Type { get; set; }
        public MessageFlags Flags { get; set; }
        public uint SequenceNumber { get; set; } = 0;
        public ulong RequestID { get; set; } = 0;
        public ushort FragmentIndex { get; set; } = 0;
        public ushort FragmentCount { get; set; } = 1;
        public ulong SessionID { get; set; } = 0;
        public ulong ChannelID { get; set; } = 0;

        public List<AdditionalMessageHeader> AdditionalHeaders { get; set; } = new();
        public record AdditionalMessageHeader(NextHeaderType Type, byte[] Value);


        /// <summary>
        /// Returns size of the whole rest of the message (excluding headers) (including hmac)
        /// </summary>
        public int PayloadSize
            => MessageLength - (int)((ICdpSerializable<CommonHeader>)this).CalcSize();


        #region Flags
        public const int FlagsOffset = 6;

        public bool HasFlag(MessageFlags flag)
            => (Flags & flag) != 0;
        #endregion

        #region Session
        public bool ExistingSession
            => (SessionID >> 32) > 0;

        public const long SessionIdHostFlag = 0x80000000;
        public void CorrectClientSessionBit()
            => SessionID = SessionID ^ SessionIdHostFlag;
        #endregion

        #region Message Length
        public const int MessageLengthOffset = 2;
        public void SetMessageLength(int payloadSize)
        {
            MessageLength = (ushort)(payloadSize + ((ICdpSerializable<CommonHeader>)this).CalcSize());
        }
        #endregion

        public void SetReplyToId(ulong requestId, bool userMessage = false)
        {
            AdditionalHeaders.RemoveAll((x) => x.Type == NextHeaderType.ReplyToId);

            byte[] value = new byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(value, requestId);

            AdditionalHeaders.Add(new(
                userMessage ? NextHeaderType.UserMessageRequestId : NextHeaderType.ReplyToId,
                value
            ));
        }
    }
}
