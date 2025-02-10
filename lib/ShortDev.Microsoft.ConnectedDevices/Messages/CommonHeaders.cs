using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ShortDev.Microsoft.ConnectedDevices.Messages;

/// <summary>
/// The <see cref="CommonHeader"/> is common for all Messages.
/// </summary>
public sealed class CommonHeader : IBinaryWritable, IBinaryParsable<CommonHeader>
{
    public static CommonHeader Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        if (!TryParse(ref reader, out var result, out var ex))
            throw ex;
        return result;
    }

    public static bool TryParse<TReader>(ref TReader reader, [MaybeNullWhen(false)] out CommonHeader result, [MaybeNullWhen(true)] out Exception ex) where TReader : IEndianReader, allows ref struct
    {
        result = new();
        var sig = reader.ReadUInt16();
        if (sig != Constants.Signature)
        {
            ex = new InvalidDataException($"Wrong signature. Expected \"{Constants.Signature}\"");
            return false;
        }

        result.MessageLength = reader.ReadUInt16();
        result.Version = reader.ReadUInt8();
        if (result.Version != Constants.ProtocolVersion)
        {
            ex = new InvalidDataException($"Wrong version. Got \"{result.Version}\", expected \"{Constants.ProtocolVersion}\"");
            return false;
        }

        result.Type = (MessageType)reader.ReadUInt8();
        result.Flags = (MessageFlags)reader.ReadInt16();
        result.SequenceNumber = reader.ReadUInt32();
        result.RequestID = reader.ReadUInt64();
        result.FragmentIndex = reader.ReadUInt16();
        result.FragmentCount = reader.ReadUInt16();
        result.SessionId = reader.ReadUInt64();
        result.ChannelId = reader.ReadUInt64();

        AdditionalHeaderType nextHeaderType;
        byte nextHeaderSize;
        List<AdditionalHeader> additionalHeaders = [];
        while (true)
        {
            nextHeaderType = (AdditionalHeaderType)reader.ReadUInt8();
            nextHeaderSize = reader.ReadUInt8();

            if (nextHeaderType == AdditionalHeaderType.None)
                break;

            byte[] value = new byte[nextHeaderSize];
            reader.ReadBytes(value);
            additionalHeaders.Add(new(nextHeaderType, value));
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


    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
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
            writer.Write(header.Value.Span);
        }
        writer.Write((byte)AdditionalHeaderType.None);
        writer.Write((byte)0);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal int CalcSize()
    {
        int size =
            sizeof(ushort) + // Constants.Signature
            sizeof(ushort) + // MessageLength
            sizeof(byte) + // Constants.ProtocolVersion
            sizeof(byte) + // Type
            sizeof(short) + // Flags
            sizeof(uint) + // SequenceNumber
            sizeof(ulong) + // RequestID
            sizeof(ushort) + // FragmentIndex
            sizeof(ushort) + // FragmentCount
            sizeof(ulong) + // SessionId
            sizeof(ulong); // ChannelId

        foreach (var header in AdditionalHeaders)
        {
            size +=
                sizeof(byte) + // Type
                sizeof(byte) + // Value.Length
                header.Value.Length; // Value
        }

        return size +
            sizeof(byte) + // AdditionalHeaderType.None
            sizeof(byte); // 0
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
    public List<AdditionalHeader> AdditionalHeaders { get; set; } = [];


    /// <summary>
    /// Returns size of the whole rest of the message (excluding headers) (including hmac)
    /// </summary>
    public int PayloadSize
        => MessageLength - (int)CalcSize();


    #region Flags
    public const int FlagsOffset = 6;

    public bool HasFlag(MessageFlags flag)
        => (Flags & flag) != 0;
    #endregion

    #region Session

    public const ulong SessionIdHostFlag = 0x80000000;
    public void CorrectClientSessionBit()
        => SessionId ^= SessionIdHostFlag;
    #endregion

    #region Message Length
    public const int MessageLengthOffset = 2;
    public void SetPayloadLength(int payloadSize)
    {
        MessageLength = (ushort)(payloadSize + CalcSize());
    }

    public static void ModifyMessageLength(Span<byte> msgBuffer, short delta)
    {
        var msgLengthSpan = msgBuffer.Slice(MessageLengthOffset, 2);
        ReplaceMessageLength(
            msgBuffer,
            (short)(BinaryPrimitives.ReadInt16BigEndian(msgLengthSpan) + delta)
        );
    }

    public static void ReplaceMessageLength(Span<byte> msgBuffer, short msgLength)
    {
        var msgLengthSpan = msgBuffer.Slice(MessageLengthOffset, 2);
        BinaryPrimitives.WriteInt16BigEndian(
            msgLengthSpan,
            msgLength
        );
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

    public AdditionalHeader? TryGetHeader(AdditionalHeaderType type)
        => AdditionalHeaders.FirstOrDefault(x => x.Type == type);

    public ulong? TryGetReplyToId()
    {
        var header = TryGetHeader(AdditionalHeaderType.ReplyToId);
        if (header == null)
            return null;

        return BinaryPrimitives.ReadUInt64LittleEndian(header.Value.Span);
    }
}
