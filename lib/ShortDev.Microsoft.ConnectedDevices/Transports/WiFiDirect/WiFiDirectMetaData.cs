using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
using System.Net.NetworkInformation;
using System.Security.Cryptography;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;
internal static class WiFiDirectMetaData
{
    const byte Version = 1;

    static void WriteHeader(ref EndianWriter writer, MessageType messageType, PhysicalAddress deviceAddress)
    {
        writer.Write(Version);
        writer.Write((byte)messageType);
        WriteField(ref writer, MessageValueType.DeviceAddress, deviceAddress.GetAddressBytes());
    }
    static void WriteField(ref EndianWriter writer, MessageValueType type, scoped ReadOnlySpan<byte> value)
    {
        writer.Write((byte)type);
        writer.WriteWithLength(value);
    }

    public static EndpointMetadata GetUpgradeRequest(PhysicalAddress deviceAddress, RolePreference rolePreference = RolePreference.Client)
    {
        EndianWriter writer = new(Endianness.BigEndian);
        WriteHeader(ref writer, MessageType.ClientAvailableForUpgrade, deviceAddress);
        WriteField(ref writer, MessageValueType.RolePreference, [(byte)rolePreference]);
        return new(CdpTransportType.WifiDirect, writer.Buffer.ToArray());
    }

    public static EndpointMetadata GetUpgradeEndpoints(PhysicalAddress deviceAddress)
    {
        var rawAddress = deviceAddress.GetAddressBytes();
        return new(CdpTransportType.WifiDirect, [
            Version,
            (byte)MessageType.HostGetUpgradeEndpoints,
            (byte)MessageValueType.DeviceAddress, (byte)rawAddress.Length, ..rawAddress,
            (byte)MessageValueType.RoleDecision, /*size*/ 0x01, 0x00,
            (byte)MessageValueType.GOPreSharedKey, /*size*/ 0x06, ..RandomNumberGenerator.GetBytes(6),
            (byte)MessageValueType.GOSSID, /*size*/ 0x06, ..RandomNumberGenerator.GetBytes(6),
        ]);
    }

    static void ReadHeader(ref EndianReader reader, out MessageType messageType, out PhysicalAddress deviceAddress)
    {
        byte version = reader.ReadByte();
        if (version != Version)
            throw new InvalidOperationException($"Unexpected version {version}");

        messageType = (MessageType)reader.ReadByte();
        deviceAddress = new(reader.ReadBytesWithLength().ToArray());
    }

    enum MessageType : byte
    {
        Invalid,
        ClientAvailableForUpgrade,
        HostGetUpgradeEndpoints,
        ClientFinalizeUpgrade
    }

    enum MessageValueType : byte
    {
        InvalidMessageValueType,
        RolePreference,
        RoleDecision,
        DeviceAddress,
        GODeviceAddress,
        GOSSID,
        GOPreSharedKey
    }

    public enum RolePreference : byte
    {
        GroupOwner = 1,
        Client
    }
}
