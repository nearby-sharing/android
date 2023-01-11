using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Messages;

/// <summary>
/// The <see cref="CommonHeader"/> is common for all Messages.
/// </summary>
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
        result.SessionId = reader.ReadUInt64();
        result.ChannelId = reader.ReadUInt64();

        AdditionalHeaderType nextHeaderType;
        byte nextHeaderSize;
        List<AdditionalHeader> additionalHeaders = new();
        while (true)
        {
            nextHeaderType = (AdditionalHeaderType)reader.ReadByte();
            nextHeaderSize = reader.ReadByte();

            if (nextHeaderType != AdditionalHeaderType.None)
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
        writer.Write(SessionId);
        writer.Write(ChannelId);

        foreach (var header in AdditionalHeaders)
        {
            writer.Write((byte)header.Type);
            writer.Write((byte)header.Value.Length);
            writer.Write(header.Value);
        }
        writer.Write((byte)AdditionalHeaderType.None);
        writer.Write((byte)0);
    }

    /// <summary>
    /// Entire message length in bytes including signature.
    /// </summary>
    public ushort MessageLength { get; set; }
    /// <summary>
    /// Protocol version the sender is using. For this protocol version, this value is always 3. <br/>
    /// Lower values indicate older versions of the protocol.
    /// </summary>
    public byte Version { get; set; } = Constants.ProtocolVersion;
    /// <summary>
    /// Indicates current message type.
    /// </summary>
    public MessageType Type { get; set; }
    /// <summary>
    /// A value describing the message properties.
    /// </summary>
    public MessageFlags Flags { get; set; }
    /// <summary>
    /// Current message number for this session.
    /// </summary>
    public uint SequenceNumber { get; set; } = 0;
    /// <summary>
    /// A monotonically increasing number, generated on the sending side, that uniquely identifies the message. <br/>
    /// It can then be used to correlate response messages to their corresponding request messages. <br/>
    /// <br/>
    /// (See <see cref="SetReplyToId(ulong)"/>) <br/>
    /// (See <see cref="AdditionalHeaderType.ReplyToId"/> and <see cref="AdditionalHeaderType.UserMessageRequestId"/>)
    /// </summary>
    public ulong RequestID { get; set; } = 0;
    /// <summary>
    /// Current fragment for current message.
    /// </summary>
    public ushort FragmentIndex { get; set; } = 0;
    /// <summary>
    /// Number of total fragments for current message.
    /// </summary>
    public ushort FragmentCount { get; set; } = 1;
    /// <summary>
    /// ID representing the session.
    /// </summary>
    public ulong SessionId { get; set; } = 0;
    /// <summary>
    /// Zero if the <see cref="SessionId"/> is zero.
    /// </summary>
    public ulong ChannelId { get; set; } = 0;

    /// <summary>
    /// If an additional header record is included, this value indicates the type. <br/>
    /// Some values are implementation-specific.
    /// </summary>
    public List<AdditionalHeader> AdditionalHeaders { get; set; } = new();


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

    public const ulong SessionIdHostFlag = 0x80000000;
    public void CorrectClientSessionBit()
        => SessionId = SessionId ^ SessionIdHostFlag;
    #endregion

    #region Message Length
    public const int MessageLengthOffset = 2;
    public void SetMessageLength(int payloadSize)
    {
        MessageLength = (ushort)(payloadSize + ((ICdpSerializable<CommonHeader>)this).CalcSize());
    }
    #endregion

    public void SetReplyToId(ulong requestId)
    {
        AdditionalHeaders.RemoveAll((x) => x.Type == AdditionalHeaderType.ReplyToId);

        byte[] value = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(value, requestId);

        AdditionalHeaders.Add(new(
            AdditionalHeaderType.ReplyToId,
            value
        ));
    }
}
