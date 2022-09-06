using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using ShortDev.Networking;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery
{
    public sealed class BluetoothAdvertisement : IDiscovery
    {
        public ICdpBluetoothHandler Handler { get; }
        public BluetoothAdvertisement(ICdpBluetoothHandler handler)
        {
            Handler = handler;
        }

        CancellationTokenSource? cancellationTokenSource;
        public async void StartDiscovery()
        {
            StopDiscovery();
            cancellationTokenSource = new();

            await Handler.ScanForDevicesAsync(new()
            {
                ScanTime = TimeSpan.FromSeconds(30),
                OnDeviceDiscovered = (device) =>
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                        return;

                    if (!TryParseBLeData(device, out var data))
                        return;

                    System.Diagnostics.Debug.Print(data!.ToString());
                }
            }, cancellationTokenSource.Token);
        }

        public void StopDiscovery()
        {
            // Called from "StartDiscovery"!
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
        }

        public static bool TryParseBLeData(CdpBluetoothDevice device, out BeaconData? data)
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

        public static byte[] GenerateAdvertisement(PhysicalAddress macAddress, DeviceType deviceType, string deviceName)
        {
            using (MemoryStream stream = new())
            using (BinaryWriter writer = new(stream))
            {
                writer.Write((byte)0x1);
                writer.Write((byte)deviceType);
                writer.Write((byte)0x21);
                writer.Write((byte)0x0a);
                writer.Write(BinaryConvert.Reverse(macAddress.GetAddressBytes()));
                writer.Write(Encoding.UTF8.GetBytes(deviceName));

                return stream.ToArray();
            }
        }

        public const int ManufacturerId = 0x6; // Microsoft
        public const string ServiceId = "c7f94713-891e-496a-a0e7-983a0946126e";
        public const string ServiceName = "CDP Proximal Transport";

        public record BeaconData(DeviceType DeviceType, PhysicalAddress? MacAddress, string? DeviceName);
    }
}
