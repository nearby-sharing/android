using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Runtime;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;
using System.Net.NetworkInformation;
using BLeScanResult = Android.Bluetooth.LE.ScanResult;

namespace NearShare.Handlers;

public sealed class AndroidBluetoothHandler(BluetoothAdapter adapter, PhysicalAddress macAddress) : IBluetoothHandler
{
    public BluetoothAdapter Adapter { get; } = adapter;
    public bool IsEnabled => Adapter.IsEnabled;
    public PhysicalAddress MacAddress { get; } = macAddress;

    private BluetoothLeScanner BleScanner
        => Adapter?.BluetoothLeScanner ?? throw new InvalidOperationException($"\"{nameof(Adapter)}\" is not initialized");

    private BluetoothLeAdvertiser BleAdvertiser
        => Adapter?.BluetoothLeAdvertiser ?? throw new InvalidOperationException($"\"{nameof(Adapter)}\" is not initialized");

    #region BLe Scan
    BleScannerCallback? _scanningCallback;
    public ValueTask StartScanBle(ScanOptions scanOptions, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _scanningCallback, value: new()) is not null)
            throw new InvalidOperationException("Scan is already running");

        _scanningCallback.OnFoundDevice += (result) =>
        {
            try
            {
                var beaconData = result.ScanRecord?.GetManufacturerSpecificData(Constants.BLeBeaconManufacturerId);
                if (beaconData != null && BLeBeacon.TryParse(beaconData, out var data))
                    scanOptions.OnDeviceDiscovered?.Invoke(data, result.Rssi);
            }
            catch { }
        };
        BleScanner.StartScan(_scanningCallback);

        return ValueTask.CompletedTask;
    }

    public ValueTask StopScanBle(CancellationToken cancellationToken)
    {
        var callback = Volatile.Read(ref _scanningCallback) ?? throw new InvalidOperationException("Scan is not running");

        BleScanner.StopScan(callback);
        Volatile.Write(ref _scanningCallback, null);

        return ValueTask.CompletedTask;
    }

    sealed class BleScannerCallback : ScanCallback
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

    public async Task<CdpSocket> ConnectRfcommAsync(EndpointInfo endpoint, RfcommOptions options, CancellationToken cancellationToken = default)
    {
        if (Adapter == null)
            throw new InvalidOperationException($"{nameof(Adapter)} is not initialized!");

        var btDevice = Adapter.GetRemoteDevice(endpoint.Address) ?? throw new ArgumentException($"Could not find bt device with address \"{endpoint.Address}\"");
        var btSocket = btDevice.CreateInsecureRfcommSocketToServiceRecord(Java.Util.UUID.FromString(options.ServiceId)) ?? throw new ArgumentException("Could not create service socket");
        await btSocket.ConnectAsync().ConfigureAwait(false);
        return btSocket.ToCdp();
    }

    #region BLe Advertisement
    BLeAdvertiseCallback? _advertiserCallback;
    public ValueTask StartAdvertiseBle(AdvertiseOptions options, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _advertiserCallback, value: new()) is not null)
            throw new InvalidOperationException("Advertise is already running");

        var settings = new AdvertiseSettings.Builder()
            .SetAdvertiseMode(AdvertiseMode.LowLatency)!
            .SetTxPowerLevel(AdvertiseTx.PowerHigh)!
            .SetConnectable(false)!
            .Build();

        var data = new AdvertiseData.Builder()
            .AddManufacturerData(options.ManufacturerId, options.BeaconData.ToArray())!
            .Build();

        BleAdvertiser.StartAdvertising(settings, data, _advertiserCallback);

        return ValueTask.CompletedTask;
    }

    public ValueTask StopAdvertiseBle(CancellationToken cancellationToken)
    {
        var callback = Volatile.Read(ref _advertiserCallback) ?? throw new InvalidOperationException("Advertise is not running");

        BleAdvertiser.StopAdvertising(callback);
        Volatile.Write(ref _advertiserCallback, null);

        return ValueTask.CompletedTask;
    }

    class BLeAdvertiseCallback : AdvertiseCallback { }
    #endregion

    #region Rfcomm
    BackgroundAction? _rfcommListenTask;
    public ValueTask StartListenRfcomm(RfcommOptions options, CancellationToken cancellationToken = default)
        => BackgroundAction.Start(ref _rfcommListenTask, token => ListenRfcomm(options, token), cancellationToken);

    async Task ListenRfcomm(RfcommOptions options, CancellationToken cancellationToken)
    {
        using var listener = Adapter.ListenUsingInsecureRfcommWithServiceRecord(
            options.ServiceName,
            Java.Util.UUID.FromString(options.ServiceId)
        )!;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var socket = await listener.AcceptAsync();
                if (cancellationToken.IsCancellationRequested)
                {
                    socket?.Close();
                    return;
                }

                if (socket != null)
                    options.SocketConnected(socket.ToCdp());
            }
        }
        finally
        {
            listener.Close();
        }
    }

    public async ValueTask StopListenRfcomm(CancellationToken cancellationToken)
        => await BackgroundAction.Stop(ref _rfcommListenTask, cancellationToken);
    #endregion
}
