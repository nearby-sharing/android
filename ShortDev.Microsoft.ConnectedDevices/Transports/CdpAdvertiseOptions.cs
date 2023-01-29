using ShortDev.Networking;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public sealed record CdpAdvertisement(DeviceType DeviceType, PhysicalAddress MacAddress, string DeviceName)
{
    enum BeaconFlags : byte
    {
        MyDevice,
        Public
    }

    public static bool TryParse(byte[] beaconData, [MaybeNullWhen(false)] out CdpAdvertisement data)
    {
        data = null;

        if (beaconData == null)
            return false;

        using (MemoryStream stream = new(beaconData))
        using (BigEndianBinaryReader reader = new(stream))
        {
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

            /* Reserved */
            reader.ReadByte();

            data = new(
                deviceType,
                new PhysicalAddress(BinaryConvert.Reverse(reader.ReadBytes(6))),
                Encoding.UTF8.GetString(reader.ReadBytes((int)(stream.Length - stream.Position)))
            );
        }
        return true;
    }

    public byte[] GenerateBLeBeacon()
    {
        using (MemoryStream stream = new())
        using (BinaryWriter writer = new(stream))
        {
            writer.Write((byte)0x1);
            writer.Write((byte)DeviceType);
            writer.Write((byte)0x21);
            writer.Write((byte)0x0a);
            writer.Write(BinaryConvert.Reverse(MacAddress.GetAddressBytes()));
            writer.Write(Encoding.UTF8.GetBytes(DeviceName));

            return stream.ToArray();
        }
    }
}
