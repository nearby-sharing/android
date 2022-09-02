using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using System;
using System.IO;
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
                ScanTime = TimeSpan.FromSeconds(10),
                OnDeviceDiscovered = (device) =>
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                        return;

                    if (!TryParseBLeData(device, out var data))
                        return;


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

        bool TryParseBLeData(CdpBluetoothDevice device, out BeaconData? data)
        {
            data = null;

            if (device.BeaconData == null)
                return false;

            using (MemoryStream stream = new(device.BeaconData))
            using (BinaryReader reader = new(stream))
            {
                var length = reader.ReadByte();
                if (length != 30)
                    return false;

                var verifyByte = reader.ReadByte();
                if (verifyByte != 0xff)
                    return false;

                var msId = reader.ReadInt16();
                if (msId != 6)
                    return false;

                var scenarioType = reader.ReadByte();
                if (scenarioType != 1)
                    return false;

                var versionAndDeviceType = reader.ReadByte();
                var deviceType = (DeviceType)versionAndDeviceType;
                var versionAndFlags = reader.ReadByte();
                /* Reserved */
                reader.ReadByte();
                byte[] salt = reader.ReadBytes(4);
                byte[] deviceHash = reader.ReadBytes(16);

                data = new()
                {
                    DeviceType = deviceType,
                    Salt = salt,
                    DeviceHash = deviceHash
                };
            }
            return true;
        }

        class BeaconData
        {
            public DeviceType DeviceType { get; set; }
            public byte[]? Salt { get; set; }
            public byte[]? DeviceHash { get; set; }
        }
    }
}
