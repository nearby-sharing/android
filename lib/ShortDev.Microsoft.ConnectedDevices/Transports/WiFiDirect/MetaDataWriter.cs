using System.Net.NetworkInformation;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;

static class MetaDataWriter
{
    const byte Version = 1;

    public static void ParseHostResponse(byte[] data, out PhysicalAddress deviceAddress, out string ssid, out ReadOnlyMemory<byte> sharedKey)
    {
        var reader = EndianReader.FromSpan(Endianness.BigEndian, data);
        ReadHeader(ref reader, MessageType.HostGetUpgradeEndpoints, out deviceAddress);
        ReadField(ref reader, MessageValueType.RoleDecision, out var role);
        if ((GroupRole)role[0] != GroupRole.GroupOwner)
            throw new InvalidOperationException("Expected GroupOwner role");

        ReadField(ref reader, MessageValueType.GOPreSharedKey, out var sharedKeySpan);
        sharedKey = sharedKeySpan.ToArray();

        ReadField(ref reader, MessageValueType.GOSSID, out var ssidSpan);
        ssid = Encoding.UTF8.GetString(ssidSpan);
    }

    #region Write
    internal static void WriteHeader<TWriter>(ref TWriter writer, MessageType messageType, PhysicalAddress deviceAddress) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write(Version);
        writer.Write((byte)messageType);
        WriteField(ref writer, MessageValueType.DeviceAddress, deviceAddress.GetAddressBytes());
    }
    internal static void WriteField<TWriter>(ref TWriter writer, MessageValueType type, scoped ReadOnlySpan<byte> value) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.Write((byte)type);
        writer.WriteWithLength(value);
    }
    #endregion

    #region Parse
    internal static void ReadHeader<TReader>(ref TReader reader, MessageType expectedMessageType, out PhysicalAddress deviceAddress) where TReader : struct, IEndianReader, allows ref struct
    {
        byte version = reader.ReadUInt8();
        if (version != Version)
            throw new InvalidOperationException($"Unexpected version {version}");

        var messageType = (MessageType)reader.ReadUInt8();
        if (messageType != expectedMessageType)
            throw new InvalidOperationException($"Expected {expectedMessageType}");

        ReadField(ref reader, MessageValueType.DeviceAddress, out var deviceAddressRaw);
        deviceAddress = new(deviceAddressRaw.ToArray());
    }

    internal static void ReadField<TReader>(ref TReader reader, MessageValueType expectedType, out ReadOnlySpan<byte> data) where TReader : struct, IEndianReader, allows ref struct
    {
        var type = (MessageValueType)reader.ReadUInt8();
        if (type != expectedType)
            throw new InvalidOperationException($"Unexpected {type}");

        data = reader.ReadBytesWithLength().Span;
    }
    #endregion
}
