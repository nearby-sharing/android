#nullable enable

using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using Nearby_Sharing_Windows.Bluetooth;
using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using ShortDev.Networking;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using ManifestPermission = Android.Manifest.Permission;
using Stream = System.IO.Stream;

namespace Nearby_Sharing_Windows
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme")]
    public class ReceiveActivity : AppCompatActivity, ICdpBluetoothHandler
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            RequestPermissions(new[] {
                ManifestPermission.AccessFineLocation,
                ManifestPermission.AccessCoarseLocation,
                ManifestPermission.Bluetooth,
                ManifestPermission.BluetoothScan,
                ManifestPermission.BluetoothConnect,
                ManifestPermission.BluetoothAdvertise,
                ManifestPermission.AccessBackgroundLocation
            }, 0);

            UdpAdvertisement advertisement = new();
            advertisement.StartDiscovery();

            BluetoothAdvertisement bluetoothAdvertisement = new(this);
            //bluetoothAdvertisement.StartDiscovery();
            //return;

            var service = (BluetoothManager)GetSystemService(BluetoothService)!;
            var adapter = service.Adapter!;

            var settings = new AdvertiseSettings.Builder()
                .SetAdvertiseMode(AdvertiseMode.LowLatency)!
                .SetTxPowerLevel(AdvertiseTx.PowerHigh)!
                .SetConnectable(false)!
                .Build();

            var data = new AdvertiseData.Builder()
                .AddManufacturerData(
                    BluetoothAdvertisement.ManufacturerId, 
                    BluetoothAdvertisement.GenerateAdvertisement(
                        PhysicalAddress.Parse("00:fa:21:3e:fb:18".Replace(":", "").ToUpper()), 
                        DeviceType.Android, 
                        adapter.Name!
                    ))!
                .Build();

            adapter.BluetoothLeAdvertiser!.StartAdvertising(settings, data, new Callback());

            var rfcommListener = adapter.ListenUsingRfcommWithServiceRecord(BluetoothAdvertisement.ServiceName, Java.Util.UUID.FromString(BluetoothAdvertisement.ServiceId))!;
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var socket = await rfcommListener.AcceptAsync();
                    PrintStreamData(socket.InputStream);
                }
            });
        }

        public static void PrintStreamData(Stream stream)
        {
            using (BigEndianBinaryReader reader = new(stream))
            {
                CommonHeaders headers = new();
                if (headers.TryRead(reader))
                {

                }
            }
        }

        class Callback : AdvertiseCallback { }

        public async Task ScanForDevicesAsync(CdpScanOptions<CdpBluetoothDevice> scanOptions, CancellationToken cancellationToken = default)
        {
            var service = (BluetoothManager)GetSystemService(BluetoothService)!;
            var adapter = service.Adapter!;
            {
                var scanner = adapter.BluetoothLeScanner!;

                BluetoothLeScannerCallback scanningCallback = new();
                scanningCallback.OnFoundDevice += (result) =>
                {
                    var device = adapter.GetRemoteDevice(result.Device!.Address)!;
                    var beaconData = result.ScanRecord!.GetBytes();
                    beaconData = result.ScanRecord.GetManufacturerSpecificData(6);
                    scanOptions.OnDeviceDiscovered?.Invoke(new()
                    {
                        Name = device.Name,
                        Alias = device.Alias,
                        Address = device.Address,
                        BeaconData = beaconData
                    });
                };

                scanner.StartScan(scanningCallback);
                scanner.FlushPendingScanResults(scanningCallback);
                //await Task.Delay(scanOptions.ScanTime);
                //scanner.StopScan(scanningCallback);
            }
        }

        public async Task ConnectAsync(CdpBluetoothDevice device, CancellationToken cancellationToken = default)
        {
            var service = await BluetoothLeServiceConnection.ConnectToServiceAsync(this);
            if (!service.TryInitialize())
                return;

            service.Connect(device.Address);
        }

        class BluetoothLeScannerCallback : ScanCallback
        {
            public event Action<ScanResult>? OnFoundDevice;

            public override void OnScanResult([GeneratedEnum] ScanCallbackType callbackType, ScanResult? result)
            {
                if (result != null)
                    OnFoundDevice?.Invoke(result);
            }

            public override void OnBatchScanResults(IList<ScanResult>? results)
            {
                if (results != null)
                    foreach (var result in results)
                        if (result != null)
                            OnFoundDevice?.Invoke(result);
            }
        }
    }
}