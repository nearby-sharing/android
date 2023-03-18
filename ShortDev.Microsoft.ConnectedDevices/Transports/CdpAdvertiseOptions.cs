using ShortDev.Networking;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public sealed record CdpAdvertisement(DeviceType DeviceType, PhysicalAddress MacAddress, string DeviceName)
{
    [Flags]
    enum BeaconFlags : byte
    {
        MyDevice,
        Public
    }

    [Flags]
    enum SessionPolicy
    {
        RemoteSessionsHosted = 1,
        RemoteSessionsNotHosted = 2,
        NearShareAuthPolicySameUser = 4,
        NearShareAuthPolicyPermissive = 8,
        NearShareAuthPolicyFamily = 0x10
    }

    public static bool TryParse(byte[] beaconData, [MaybeNullWhen(false)] out CdpAdvertisement data)
    {
        data = null;

        if (beaconData == null)
            return false;

        EndianReader reader = new(Endianness.BigEndian, beaconData);

        var scenarioType = reader.ReadByte();
        if (scenarioType != 1)
            return false;

        var deviceType = (DeviceType)reader.ReadByte();

        var versionAndFlags = reader.ReadByte();
        if (versionAndFlags >> 5 != 1)
            return false; // wrong version

        var flags = (BeaconFlags)(versionAndFlags & 0x1f);
        if ((int)flags >= 2)
            return false; // wrong flags

        if (flags != BeaconFlags.Public)
            return false;

        var policy = (SessionPolicy)reader.ReadByte();

        data = new(
            deviceType,
            new PhysicalAddress(BinaryConvert.ToReversed(reader.ReadBytes(6))),
            Encoding.UTF8.GetString(reader.ReadToEnd())
        );

        return true;
    }

    public byte[] GenerateBLeBeacon()
    {
        EndianWriter writer = new(Endianness.LittleEndian);
        writer.Write((byte)0x1);
        writer.Write((byte)DeviceType);
        writer.Write((byte)0x21);
        writer.Write((byte)0x0a);
        writer.Write(BinaryConvert.ToReversed(MacAddress.GetAddressBytes()));
        writer.Write(DeviceName);

        return writer.Buffer.ToArray();
    }
}
