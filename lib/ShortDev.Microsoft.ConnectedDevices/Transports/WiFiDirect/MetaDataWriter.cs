using System.Net.NetworkInformation;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;
static class MetaDataWriter
{
    const byte Version = 1;

    public static void ParseHostResponse(byte[] data, out PhysicalAddress deviceAddress, out string ssid, out string sharedKey)
    {
        EndianReader reader = new(Endianness.BigEndian, data);
        ReadHeader(ref reader, MessageType.HostGetUpgradeEndpoints, out deviceAddress);
        ReadField(ref reader, MessageValueType.RoleDecision, out var role);
        if ((GroupRole)role[0] != GroupRole.GroupOwner)
            throw new InvalidOperationException("Expected GroupOwner role");

        ReadField(ref reader, MessageValueType.GOPreSharedKey, out var sharedKeySpan);
        sharedKey = Convert.ToHexString(sharedKeySpan);

        ReadField(ref reader, MessageValueType.GOSSID, out var ssidSpan);
        ssid = Encoding.UTF8.GetString(ssidSpan);
    }

    #region Write
    internal static void WriteHeader(ref EndianWriter writer, MessageType messageType, PhysicalAddress deviceAddress)
    {
        writer.Write(Version);
        writer.Write((byte)messageType);
        WriteField(ref writer, MessageValueType.DeviceAddress, deviceAddress.GetAddressBytes());
    }
    internal static void WriteField(ref EndianWriter writer, MessageValueType type, scoped ReadOnlySpan<byte> value)
    {
        writer.Write((byte)type);
        writer.WriteWithLength(value);
    }
    #endregion

    #region Parse
    internal static void ReadHeader(ref EndianReader reader, MessageType expectedMessageType, out PhysicalAddress deviceAddress)
    {
        byte version = reader.ReadByte();
        if (version != Version)
            throw new InvalidOperationException($"Unexpected version {version}");

        var messageType = (MessageType)reader.ReadByte();
        if (messageType != expectedMessageType)
            throw new InvalidOperationException($"Expected {expectedMessageType}");

        ReadField(ref reader, MessageValueType.DeviceAddress, out var deviceAddressRaw);
        deviceAddress = new(deviceAddressRaw.ToArray());
    }

    internal static void ReadField(ref EndianReader reader, MessageValueType expectedType, out ReadOnlySpan<byte> data)
    {
        var type = (MessageValueType)reader.ReadByte();
        if (type != expectedType)
            throw new InvalidOperationException($"Unexpected {type}");

        data = reader.ReadBytesWithLength();
    }
    #endregion
}
