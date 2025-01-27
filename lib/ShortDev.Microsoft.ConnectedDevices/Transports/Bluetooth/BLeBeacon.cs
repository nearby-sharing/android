using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;

public sealed record BLeBeacon(DeviceType DeviceType, PhysicalAddress MacAddress, string DeviceName)
{
    [Flags]
    enum BeaconFlags : byte
    {
        MyDevice,
        Public
    }

    public static bool TryParse(byte[] beaconData, [MaybeNullWhen(false)] out BLeBeacon data)
    {
        data = null;

        if (beaconData == null)
            return false;

        var reader = EndianReader.Create(Endianness.BigEndian, beaconData);

        var scenarioType = (ScenarioType)reader.ReadByte();
        if (scenarioType != ScenarioType.Bluetooth)
            return false;

        var deviceType = (DeviceType)reader.ReadByte();

        var versionAndFlags = reader.ReadByte();
        if (versionAndFlags >> 5 != 1)
            return false; // wrong version

        var flags = (BeaconFlags)(versionAndFlags & 0x1f);
        if ((int)flags >= 2)
            return false; // wrong flags

        /* deviceStatus */
        _ = (ExtendedDeviceStatus)reader.ReadByte();

        if (flags != BeaconFlags.Public)
            return false;

        data = new(
            deviceType,
            new PhysicalAddress(reader.ReadBytes(6).ToReversed().ToArray()),
            Encoding.UTF8.GetString(reader.ReadToEnd())
        );

        return true;
    }

    public byte[] ToArray()
    {
        using var writer = EndianWriter.Create(Endianness.LittleEndian, ConnectedDevicesPlatform.MemoryPool);
        writer.Write((byte)ScenarioType.Bluetooth);
        writer.Write((byte)DeviceType);

        byte versionAndFlags = (byte)BeaconFlags.Public;
        versionAndFlags |= Constants.BLeBeaconVersion << 5;
        writer.Write(versionAndFlags);

        var deviceStatus = ExtendedDeviceStatus.RemoteSessionsNotHosted | ExtendedDeviceStatus.NearShareAuthPolicyPermissive;
        writer.Write((byte)deviceStatus);

        writer.Write(MacAddress.GetAddressBytes().AsSpan().ReverseInPlace());

        // ToDo: Don't crop characters wider that 2 bytes!
        ReadOnlySpan<byte> deviceNameBuffer = Encoding.UTF8.GetBytes(DeviceName);
        var deviceNameLength = Math.Min(deviceNameBuffer.Length, Constants.BLeBeaconDeviceNameMaxByteLength);
        writer.Write(deviceNameBuffer[..deviceNameLength]);

        return writer.Buffer.WrittenSpan.ToArray();
    }
}
