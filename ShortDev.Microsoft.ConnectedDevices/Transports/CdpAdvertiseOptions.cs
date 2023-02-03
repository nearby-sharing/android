using ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;
using ShortDev.Networking;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public sealed record CdpAdvertisement(DeviceType DeviceType, PhysicalAddress MacAddress, string DeviceName)
{
    public static bool TryParse(BluetoothDevice device, [MaybeNullWhen(false)] out CdpAdvertisement data)
    {
        data = null;

        if (device.BeaconData == null)
            return false;

        using (MemoryStream stream = new(device.BeaconData))
        using (BigEndianBinaryReader reader = new(stream))
        {
            var scenarioType = reader.ReadByte();
            if (scenarioType != 1)
                return false;

            var versionAndDeviceType = reader.ReadByte();
            var deviceType = (DeviceType)versionAndDeviceType;

            var versionAndFlags = reader.ReadByte();
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
        EndianWriter writer = new(Endianness.LittleEndian);
        writer.Write((byte)0x1);
        writer.Write((byte)DeviceType);
        writer.Write((byte)0x21);
        writer.Write((byte)0x0a);
        writer.Write(BinaryConvert.Reverse(MacAddress.GetAddressBytes()));
        writer.Write(DeviceName);

        return writer.Buffer.ToArray();
    }
}
