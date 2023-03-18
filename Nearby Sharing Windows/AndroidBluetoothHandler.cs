using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Runtime;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;
using BLeScanResult = Android.Bluetooth.LE.ScanResult;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Net.NetworkInformation;

namespace Nearby_Sharing_Windows;

public sealed class AndroidBluetoothHandler : IBluetoothHandler
{
    public BluetoothAdapter Adapter { get; }
    public ICdpPlatformHandler PlatformHandler { get; }

    public PhysicalAddress MacAddress { get; }

    public AndroidBluetoothHandler(ICdpPlatformHandler handler, BluetoothAdapter adapter, PhysicalAddress macAddress)
    {
        PlatformHandler = handler;
        Adapter = adapter;
        MacAddress = macAddress;
    }

    #region BLe Scan
    public async Task ScanBLeAsync(ScanOptions scanOptions, CancellationToken cancellationToken = default)
    {
        using var scanner = Adapter?.BluetoothLeScanner ?? throw new InvalidOperationException($"\"{nameof(Adapter)}\" is not initialized");

        BluetoothLeScannerCallback scanningCallback = new();
        scanningCallback.OnFoundDevice += (result) =>
        {
            try
            {
                var address = result.Device?.Address ?? throw new InvalidDataException("No address");
                var beaconData = result.ScanRecord?.GetManufacturerSpecificData(Constants.BLeBeaconManufacturerId);
                if (beaconData != null && CdpAdvertisement.TryParse(beaconData, out var data))
                    scanOptions.OnDeviceDiscovered?.Invoke(data);
            }
            catch (InvalidDataException) { }
        };
        scanner.StartScan(scanningCallback);

        await cancellationToken.AwaitCancellation();

        scanner.StopScan(scanningCallback);
    }

    sealed class BluetoothLeScannerCallback : ScanCallback
    {
        public event Action<BLeScanResult>? OnFoundDevice;

        public override void OnScanResult([GeneratedEnum] ScanCallbackType callbackType, BLeScanResult? result)
        {
            if (result != null)
                OnFoundDevice?.Invoke(result);
        }

        public override void OnBatchScanResults(IList<BLeScanResult>? results)
        {
            if (results != null)
                foreach (var result in results)
                    if (result != null)
                        OnFoundDevice?.Invoke(result);
        }
    }

    public async Task<CdpSocket> ConnectRfcommAsync(CdpDevice device, RfcommOptions options, CancellationToken cancellationToken = default)
    {
        if (Adapter == null)
            throw new InvalidOperationException($"{nameof(Adapter)} is not initialized!");

        var btDevice = Adapter.GetRemoteDevice(device.Endpoint.Address) ?? throw new ArgumentException($"Could not find bt device with address \"{device.Endpoint.Address}\"");
        var btSocket = btDevice.CreateRfcommSocketToServiceRecord(Java.Util.UUID.FromString(options.ServiceId)) ?? throw new ArgumentException("Could not create service socket");
        await btSocket.ConnectAsync();
        return btSocket.ToCdp();
    }
    #endregion

    #region BLe Advertisement
    public async Task AdvertiseBLeBeaconAsync(AdvertiseOptions options, CancellationToken cancellationToken = default)
    {
        var settings = new AdvertiseSettings.Builder()
            .SetAdvertiseMode(AdvertiseMode.LowLatency)!
            .SetTxPowerLevel(AdvertiseTx.PowerHigh)!
            .SetConnectable(false)!
            .Build();

        var data = new AdvertiseData.Builder()
            .AddManufacturerData(options.ManufacturerId, options.BeaconData!)!
            .Build();

        BLeAdvertiseCallback callback = new();
        Adapter!.BluetoothLeAdvertiser!.StartAdvertising(settings, data, callback);

        await cancellationToken.AwaitCancellation();

        Adapter.BluetoothLeAdvertiser.StopAdvertising(callback);
    }

    class BLeAdvertiseCallback : AdvertiseCallback { }
    #endregion

    #region Rfcomm
    public async Task ListenRfcommAsync(RfcommOptions options, CancellationToken cancellationToken = default)
    {
        if (Adapter == null)
            throw new InvalidOperationException($"{nameof(Adapter)} is null");

        using (var insecureListener = Adapter.ListenUsingInsecureRfcommWithServiceRecord(
            options.ServiceName,
            Java.Util.UUID.FromString(options.ServiceId)
        )!)
        using (var securelistener = Adapter.ListenUsingRfcommWithServiceRecord(
            options.ServiceName,
            Java.Util.UUID.FromString(options.ServiceId)
        )!)
        {
            Func<BluetoothServerSocket, Task> processor = async (listener) =>
            {
                while (true)
                {
                    var socket = await listener.AcceptAsync();
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    if (socket != null)
                        options!.SocketConnected!(socket.ToCdp());
                }
            };
            await Task.WhenAny(new[] {
                Task.Run(() => processor(securelistener), cancellationToken),
                Task.Run(() => processor(insecureListener), cancellationToken)
            });
        }
    }
    #endregion

    public void Log(int level, string message)
        => PlatformHandler.Log(level, message);
}
