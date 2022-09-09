#nullable enable

using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using AndroidX.AppCompat.App;
using ShortDev.Microsoft.ConnectedDevices.Protocol;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Discovery;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using ShortDev.Networking;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
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

            //UdpAdvertisement advertisement = new();
            //advertisement.StartDiscovery();

            //BluetoothAdvertisement bluetoothAdvertisement = new(this);
            //bluetoothAdvertisement.OnDeviceConnected += BluetoothAdvertisement_OnDeviceConnected;
            //bluetoothAdvertisement.StartAdvertisement(new CdpDeviceAdvertiseOptions(
            //    DeviceType.Android,
            //    PhysicalAddress.Parse("00:fa:21:3e:fb:18".Replace(":", "").ToUpper()),
            //    adapter.Name!
            //));

            var service = (BluetoothManager)GetSystemService(BluetoothService)!;
            var adapter = service.Adapter!;

            var settings = new AdvertiseSettings.Builder()
                .SetAdvertiseMode(AdvertiseMode.LowLatency)!
                .SetTxPowerLevel(AdvertiseTx.PowerHigh)!
                .SetConnectable(false)!
                .Build();

            var data = new AdvertiseData.Builder()
                .AddManufacturerData(
                    Constants.BLeBeaconManufacturerId,
                    BluetoothAdvertisement.GenerateAdvertisement(new(
                        DeviceType.Android,
                        PhysicalAddress.Parse("00:fa:21:3e:fb:18".Replace(":", "").ToUpper()),                        
                        adapter.Name!
                    )))!
                .Build();

            adapter.BluetoothLeAdvertiser!.StartAdvertising(settings, data, new BLeAdvertiseCallback());

            var rfcommListener = adapter.ListenUsingInsecureRfcommWithServiceRecord(Constants.RfcommServiceName, Java.Util.UUID.FromString(Constants.RfcommServiceId))!;
            while (true)
            {
                var socket = await rfcommListener.AcceptAsync();
                PrintStreamData(socket.InputStream);
            }
        }

        private void BluetoothAdvertisement_OnDeviceConnected(CdpRfcommSocket socket)
        {
            PrintStreamData(socket.InputStream!);
        }

        public static void PrintStreamData(Stream stream)
        {
            List<byte> data = new();
            using (BigEndianBinaryReader reader = new(stream))
            {
                data.AddRange(reader.ReadBytes(4));
                var msgLength = data[2] << 8 | data[3];
                data.AddRange(reader.ReadBytes(msgLength - 4));
                System.Diagnostics.Debug.Print(BinaryConvert.ToString(data.ToArray()));

                return;
                CommonHeaders headers = new();
                if (headers.TryRead(reader))
                {

                }
            }
        }

        BluetoothAdapter GetBTAdapter()
        {
            var service = (BluetoothManager)GetSystemService(BluetoothService)!;
            return service.Adapter!;
        }

        public Task ScanBLeAsync(CdpScanOptions<CdpBluetoothDevice> scanOptions, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<CdpRfcommSocket> ConnectRfcommAsync(CdpBluetoothDevice device, CdpRfcommOptions options, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public async Task AdvertiseBLeBeaconAsync(CdpAdvertiseOptions options, CancellationToken cancellationToken = default)
        {
            var adapter = GetBTAdapter();
            var settings = new AdvertiseSettings.Builder()
                .SetAdvertiseMode(AdvertiseMode.LowLatency)!
                .SetTxPowerLevel(AdvertiseTx.PowerHigh)!
                .SetConnectable(false)!
                .Build();

            var data = new AdvertiseData.Builder()
                .AddManufacturerData(options.ManufacturerId, options.BeaconData!)!
                .Build();

            BLeAdvertiseCallback callback = new();
            adapter.BluetoothLeAdvertiser!.StartAdvertising(settings, data, callback);

            await AwaitCancellation(cancellationToken);

            adapter.BluetoothLeAdvertiser.StopAdvertising(callback);
        }
        class BLeAdvertiseCallback : AdvertiseCallback { }

        public async Task ListenRfcommAsync(CdpRfcommOptions options, CancellationToken cancellationToken = default)
        {
            return;
            var adapter = GetBTAdapter();
            var listener = adapter.ListenUsingInsecureRfcommWithServiceRecord(
                options.ServiceName,
                Java.Util.UUID.FromString(options.ServiceId)
            )!;
            while (true)
            {
                var socket = await listener.AcceptAsync();
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (socket != null)
                    options!.OnSocketConnected!(socket.ToCdp());
            }
        }

        Task AwaitCancellation(CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool> promise = new();
            cancellationToken.Register(() => promise.SetResult(true));
            return promise.Task;
        }
    }

    static class Extensions
    {
        public static CdpBluetoothDevice ToCdp(this BluetoothDevice @this, byte[]? beaconData = null)
            => new()
            {
                Address = @this.Address,
                Alias = @this.Alias,
                Name = @this.Name,
                BeaconData = beaconData
            };

        public static CdpRfcommSocket ToCdp(this BluetoothSocket @this)
            => new()
            {
                InputStream = @this.InputStream,
                OutputStream = @this.OutputStream,
                RemoteDevice = @this.RemoteDevice!.ToCdp(),
                Close = () => @this.Close()
            };
    }
}