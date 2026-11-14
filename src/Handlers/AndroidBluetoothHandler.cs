using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Runtime;
using NearShare.Receive;
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
            catch { }
        };
        scanner.StartScan(scanningCallback);

        await cancellationToken.AwaitCancellation().ConfigureAwait(false);

        scanner.StopScan(scanningCallback);
    }

    sealed class BluetoothLeScannerCallback : ScanCallback
    {
        public event Action<BLeScanResult>? OnFoundDevice;

        private readonly System.Timers.Timer _timer = new() { Enabled = false, Interval = 50 };
        public BluetoothLeScannerCallback()
        {
            _timer.Elapsed += OnElapsed;
        }

        private int _delivering = 0;
        private readonly Lock _lock = new();
        private List<BLeScanResult> _pendingResults = [];

        private void OnElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            // Skip this tick if the previous delivery is still running.
            if (Interlocked.CompareExchange(ref _delivering, 1, 0) != 0)
                return;

            try
            {
                List<BLeScanResult> results;

                lock (_lock)
                {
                    if (_pendingResults.Count == 0)
                        return;

                    results = _pendingResults;
                    _pendingResults = [];
                }

                results.Sort((a, b) => b.Rssi.CompareTo(a.Rssi));

                foreach (var result in results)
                    OnFoundDevice?.Invoke(result);
            }
            finally
            {
                Volatile.Write(ref _delivering, 0);
            }
        }

        public override void OnScanResult([GeneratedEnum] ScanCallbackType callbackType, BLeScanResult? result)
        {
            if (result == null)
                return;

            lock (_lock)
                _pendingResults.Add(result);

            _timer.Start();
        }

        public override void OnBatchScanResults(IList<BLeScanResult>? results)
        {
            if (results == null)
                return;

            lock (_lock)
                _pendingResults.AddRange(results);

            _timer.Start();
        }
    }

    public async Task<CdpSocket> ConnectRfcommAsync(EndpointInfo endpoint, RfcommOptions options, CancellationToken cancellationToken = default)
    {
        if (Adapter == null)
            throw new InvalidOperationException($"{nameof(Adapter)} is not initialized!");

        var btDevice = Adapter.GetRemoteDevice(endpoint.Address) ?? throw new ArgumentException($"Could not find bt device with address \"{endpoint.Address}\"");
        var btSocket = btDevice.CreateInsecureRfcommSocketToServiceRecord(Java.Util.UUID.FromString(options.ServiceId)) ?? throw new ArgumentException("Could not create service socket");
        await btSocket.ConnectAsync().ConfigureAwait(false);
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

        await cancellationToken.AwaitCancellation().ConfigureAwait(false);

        Adapter.BluetoothLeAdvertiser.StopAdvertising(callback);
    }

    class BLeAdvertiseCallback : AdvertiseCallback { }
    #endregion

    #region Rfcomm
    public async Task ListenRfcommAsync(RfcommOptions options, CancellationToken cancellationToken = default)
    {
        if (Adapter == null)
            throw new InvalidOperationException($"{nameof(Adapter)} is null");

        var listener = Adapter.ListenUsingInsecureRfcommWithServiceRecord(
           options.ServiceName,
           Java.Util.UUID.FromString(options.ServiceId)
        )!;

        try
        {
            cancellationToken.Register(listener.Close);

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
        finally
        {
            listener?.Close();
        }
    }
    #endregion
}
