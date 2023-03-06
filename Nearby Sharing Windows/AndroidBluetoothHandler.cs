using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using Android.Runtime;
using Nearby_Sharing_Windows.Bluetooth;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using BLeScanResult = Android.Bluetooth.LE.ScanResult;

namespace Nearby_Sharing_Windows;

public sealed class AndroidBluetoothHandler : IBluetoothHandler
{
    public BluetoothAdapter Adapter { get; }
    public ICdpPlatformHandler PlatformHandler { get; }
    readonly BluetoothLeService? _leService;
    public AndroidBluetoothHandler(ICdpPlatformHandler handler, BluetoothAdapter adapter, BluetoothLeService? leService = null)
    {
        PlatformHandler = handler;
        Adapter = adapter;
        _leService = leService;
    }

    #region BLe Scan
    public async Task ScanBLeAsync(ScanOptions scanOptions, CancellationToken cancellationToken = default)
    {
        using var scanner = Adapter?.BluetoothLeScanner ?? throw new InvalidOperationException($"\"{nameof(Adapter)}\" is not initialized");

        BluetoothLeScannerCallback scanningCallback = new();
        scanningCallback.OnFoundDevice += async (result) =>
        {
            try
            {
                var address = result.Device?.Address ?? throw new InvalidDataException("No address");
                var beaconData = result.ScanRecord?.GetManufacturerSpecificData(Constants.BLeBeaconManufacturerId);
                if (beaconData != null && CdpAdvertisement.TryParse(beaconData, out var data))
                {
                    if (data.DeviceName.Contains("Lukas"))
                    {
                        scanner.StopScan(scanningCallback);

                        await Task.Delay(500);

                        _leService?.Connect(address);
                    }
                    scanOptions.OnDeviceDiscovered?.Invoke(data);
                }
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
            .AddServiceUuid(ParcelUuid.FromString(options.GattServiceId))!
            .Build();

        BLeAdvertiseCallback callback = new();
        Adapter!.BluetoothLeAdvertiser!.StartAdvertising(settings, data, callback);

        await cancellationToken.AwaitCancellation();

        Adapter.BluetoothLeAdvertiser.StopAdvertising(callback);
    }

    class BLeAdvertiseCallback : AdvertiseCallback { }
    #endregion

    #region Rfcomm
    bool IBluetoothHandler.SupportsRfcomm { get; } = true;

    public async Task<CdpSocket> ConnectRfcommAsync(CdpDevice device, RfcommOptions options, CancellationToken cancellationToken = default)
    {
        if (Adapter == null)
            throw new InvalidOperationException($"{nameof(Adapter)} is not initialized!");

        var btDevice = Adapter.GetRemoteDevice(device.Address) ?? throw new ArgumentException($"Could not find bt device with address \"{device.Address}\"");
        var btSocket = btDevice.CreateRfcommSocketToServiceRecord(Java.Util.UUID.FromString(options.ServiceId)) ?? throw new ArgumentException("Could not create service socket");
        await btSocket.ConnectAsync();
        return btSocket.ToCdp();
    }

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

    #region Gatt
    bool IBluetoothHandler.SupportsGatt { get; } = true;

    public async Task<CdpSocket> ConnectGattAsync(CdpDevice device, GattOptions options, CancellationToken cancellationToken = default)
    {
        var btDevice = Adapter.GetRemoteDevice(device.Address) ?? throw new ArgumentException("Invalid address", nameof(device));
        var gatt = btDevice.ConnectGatt(Application.Context, false, new GattCallback()) ?? throw new InvalidOperationException("Could not connect via GATT");
        gatt.DiscoverServices();

        await cancellationToken.AwaitCancellation();

        return null;
    }

    public void GattTest(Activity context, string address)
    {
        var btDevice = Adapter.GetRemoteDevice(address) ?? throw new ArgumentException("Invalid address", nameof(address));
        var gatt = btDevice.ConnectGatt(context, true, new GattCallback()) ?? throw new InvalidOperationException("Could not connect via GATT");
    }

    sealed class GattCallback : BluetoothGattCallback
    {
        public override void OnServicesDiscovered(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status)
        {
            var services = gatt.Services.ToArray();
        }

        public override void OnConnectionStateChange(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status, [GeneratedEnum] ProfileState newState)
        {
            gatt?.DiscoverServices();
        }

        public override void OnServiceChanged(BluetoothGatt gatt)
        {
            var services = gatt.Services.ToArray();
            base.OnServiceChanged(gatt);
        }

        public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
        {
            base.OnCharacteristicChanged(gatt, characteristic);
        }
    }
    #endregion

    public void Log(int level, string message)
        => PlatformHandler.Log(level, message);
}
