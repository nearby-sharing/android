using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using ShortDev.Networking;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery
{
    public sealed class BluetoothAdvertisement : IAdvertiser
    {
        public ICdpBluetoothHandler Handler { get; }
        public BluetoothAdvertisement(ICdpBluetoothHandler handler)
        {
            Handler = handler;
        }

        static bool TryParseBLeData(CdpBluetoothDevice device, out BeaconData? data)
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

        public static byte[] GenerateAdvertisement(CdpDeviceAdvertiseOptions options)
        {
            using (MemoryStream stream = new())
            using (BinaryWriter writer = new(stream))
            {
                writer.Write((byte)0x1);
                writer.Write((byte)options.DeviceType);
                writer.Write((byte)0x21);
                writer.Write((byte)0x0a);
                writer.Write(BinaryConvert.Reverse(options.MacAddress.GetAddressBytes()));
                writer.Write(Encoding.UTF8.GetBytes(options.DeviceName));

                return stream.ToArray();
            }
        }

        CancellationTokenSource? cancellationTokenSource;
        public void StartAdvertisement(CdpDeviceAdvertiseOptions options)
        {
            StopAdvertisement();
            cancellationTokenSource = new();

            _ = Handler.AdvertiseBLeBeaconAsync(
                new CdpAdvertiseOptions()
                {
                    ManufacturerId = Constants.BLeBeaconManufacturerId,
                    BeaconData = GenerateAdvertisement(options)
                },
                cancellationTokenSource.Token
            );

            _ = Handler.ListenRfcommAsync(
                new CdpRfcommOptions()
                {
                    ServiceId = Constants.RfcommServiceId,
                    ServiceName = Constants.RfcommServiceName,
                    OnSocketConnected = (socket) => OnDeviceConnected?.Invoke(socket)
                },
                cancellationTokenSource.Token
            );
        }

        public event Action<CdpRfcommSocket>? OnDeviceConnected;

        public void StopAdvertisement()
        {
            // Called from "StartAdvertisement"!
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Dispose();
            }
        }

        record BeaconData(DeviceType DeviceType, PhysicalAddress? MacAddress, string? DeviceName);
    }
}
