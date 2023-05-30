using ShortDev.Networking;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

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

        EndianReader reader = new(Endianness.BigEndian, beaconData);

        var scenarioType = reader.ReadByte();
        if (scenarioType != Constants.BLeBeaconScenarioType)
            return false;

        var deviceType = (DeviceType)reader.ReadByte();

        var versionAndFlags = reader.ReadByte();
        if (versionAndFlags >> 5 != 1)
            return false; // wrong version

        var flags = (BeaconFlags)(versionAndFlags & 0x1f);
        if ((int)flags >= 2)
            return false; // wrong flags

        var deviceStatus = (ExtendedDeviceStatus)reader.ReadByte();

        if (flags != BeaconFlags.Public)
            return false;

        data = new(
            deviceType,
            new PhysicalAddress(BinaryConvert.ToReversed(reader.ReadBytes(6))),
            Encoding.UTF8.GetString(reader.ReadToEnd())
        );

        return true;
    }

    public byte[] ToArray()
    {
        EndianWriter writer = new(Endianness.LittleEndian);
        writer.Write(Constants.BLeBeaconScenarioType);
        writer.Write((byte)DeviceType);

        byte versionAndFlags = (byte)BeaconFlags.Public;
        versionAndFlags |= Constants.BLeBeaconVersion << 5;
        writer.Write(versionAndFlags);

        var deviceStatus = ExtendedDeviceStatus.RemoteSessionsNotHosted | ExtendedDeviceStatus.NearShareAuthPolicyPermissive;
        writer.Write((byte)deviceStatus);

        writer.Write(BinaryConvert.ToReversed(MacAddress.GetAddressBytes()));

        // ToDo: Don't crop characters wider that 2 bytes!
        ReadOnlySpan<byte> deviceNameBuffer = Encoding.UTF8.GetBytes(DeviceName);
        var deviceNameLength = Math.Min(deviceNameBuffer.Length, Constants.BLeBeaconDeviceNameMaxByteLength);
        writer.Write(deviceNameBuffer[..deviceNameLength]);

        return writer.Buffer.ToArray();
    }
}
