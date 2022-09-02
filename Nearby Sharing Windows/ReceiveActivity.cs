#nullable enable

using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nearby_Sharing_Windows
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme")]
    public class ReceiveActivity : AppCompatActivity, ICdpBluetoothHandler
    {

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            UdpAdvertisement advertisement = new();
            advertisement.StartDiscovery();

            BluetoothAdvertisement bluetoothAdvertisement = new(this);
            bluetoothAdvertisement.StartDiscovery();
        }

        public async Task ScanForDevicesAsync(CdpScanOptions<CdpBluetoothDevice> scanOptions, CancellationToken cancellationToken = default)
        {
            var adapter = BluetoothAdapter.DefaultAdapter!;
            var scanner = adapter.BluetoothLeScanner!;

            BluetoothLeScannerCallback scanningCallback = new();
            scanningCallback.OnFoundDevice += (result) => scanOptions.OnDeviceDiscovered?.Invoke(new()
            {
                Name = result.Device!.Name,
                Alias = result.Device!.Alias,
                Address = result.Device!.Address,
                BeaconData = result.ScanRecord!.GetBytes()
            });
            scanner.StartScan(scanningCallback);
            await Task.Delay(scanOptions.ScanTime);
            scanner.StopScan(scanningCallback);
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