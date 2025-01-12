using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Runtime;
using ShortDev.Microsoft.ConnectedDevices;
using BLeScanResult = Android.Bluetooth.LE.ScanResult;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Net.NetworkInformation;
using ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;

namespace NearShare.Handlers;

public sealed class AndroidBluetoothHandler(BluetoothAdapter adapter, PhysicalAddress macAddress) : IBluetoothHandler
{
    public BluetoothAdapter Adapter { get; } = adapter;
    public bool IsEnabled => Adapter.IsEnabled;
    public PhysicalAddress MacAddress { get; } = macAddress;

    #region BLe Scan
    public async Task ScanBLeAsync(ScanOptions scanOptions, CancellationToken cancellationToken = default)
    {
        using var scanner = Adapter?.BluetoothLeScanner ?? throw new InvalidOperationException($"\"{nameof(Adapter)}\" is not initialized");

        BluetoothLeScannerCallback scanningCallback = new();
        scanningCallback.OnFoundDevice += (result) =>
        {
            try
            {
                var beaconData = result.ScanRecord?.GetManufacturerSpecificData(Constants.BLeBeaconManufacturerId);
                if (beaconData != null && BLeBeacon.TryParse(beaconData, out var data))
                    scanOptions.OnDeviceDiscovered?.Invoke(data, result.Rssi);
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

    public async Task<CdpSocket> ConnectRfcommAsync(EndpointInfo endpoint, RfcommOptions options, CancellationToken cancellationToken = default)
    {
        if (Adapter == null)
            throw new InvalidOperationException($"{nameof(Adapter)} is not initialized!");

        var btDevice = Adapter.GetRemoteDevice(endpoint.Address) ?? throw new ArgumentException($"Could not find bt device with address \"{endpoint.Address}\"");
        var btSocket = btDevice.CreateInsecureRfcommSocketToServiceRecord(Java.Util.UUID.FromString(options.ServiceId)) ?? throw new ArgumentException("Could not create service socket");
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
            .AddManufacturerData(options.ManufacturerId, options.BeaconData.ToArray())!
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

        using var listener = Adapter.ListenUsingInsecureRfcommWithServiceRecord(
            options.ServiceName,
            Java.Util.UUID.FromString(options.ServiceId)
        )!;

        cancellationToken.Register(() => listener.Close());

        while (!cancellationToken.IsCancellationRequested)
        {
            var socket = await listener.AcceptAsync();
            if (cancellationToken.IsCancellationRequested)
            {
                socket?.Dispose();
                return;
            }

            if (socket != null)
                options!.SocketConnected!(socket.ToCdp());
        }
    }
    #endregion
}
