using ShortDev.Microsoft.ConnectedDevices.Transports;
using ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;
using System.IO.Pipes;
using System.Net.NetworkInformation;

namespace ShortDev.Microsoft.ConnectedDevices.Test.E2E;

internal sealed class BluetoothHandler(DeviceContainer container, DeviceContainer.Device device) : IBluetoothHandler
{
    public PhysicalAddress MacAddress => PhysicalAddress.Parse(device.Address);

    public bool IsEnabled => throw new NotImplementedException();

    public Task<CdpSocket> ConnectRfcommAsync(EndpointInfo endpoint, RfcommOptions options, CancellationToken cancellationToken = default)
    {
        var device = container.FindDevice(endpoint.Address)
            ?? throw new KeyNotFoundException("Could not find device");

        return Task.FromResult(
            device.ConnectFrom(new(CdpTransportType.Rfcomm, device.Address, options.ServiceId ?? ""))
        );
    }

    #region Listen
    RfcommOptions? _listenOptions;
    public ValueTask StartListenRfcomm(RfcommOptions options, CancellationToken cancellationToken)
    {
        _listenOptions = options;
        device.ConnectionRequest += OnNewConnection;
        return ValueTask.CompletedTask;
    }

    public ValueTask StopListenRfcomm(CancellationToken cancellationToken)
    {
        device.ConnectionRequest -= OnNewConnection;
        return ValueTask.CompletedTask;
    }

    void OnNewConnection(EndpointInfo client, ref (Stream Input, Stream Output)? clientStream)
    {
        if (_listenOptions is null)
            return;

        AnonymousPipeServerStream serverInputStream = new(PipeDirection.In);
        AnonymousPipeServerStream serverOutputStream = new(PipeDirection.Out);

        // Accept connection
        clientStream = (
            new AnonymousPipeClientStream(PipeDirection.In, serverOutputStream.GetClientHandleAsString()),
            new AnonymousPipeClientStream(PipeDirection.Out, serverInputStream.GetClientHandleAsString())
        );

        _listenOptions.SocketConnected?.Invoke(new CdpSocket()
        {
            InputStream = serverInputStream,
            OutputStream = serverOutputStream,
            Endpoint = client,
            Close = () =>
            {
                serverInputStream.Dispose();
                serverOutputStream.Dispose();
            }
        });
    }
    #endregion

    #region Advertise
    public ValueTask StartAdvertiseBle(AdvertiseOptions options, CancellationToken cancellationToken = default)
    {
        var data = options.BeaconData.ToArray();
        container.Advertise(device, (uint)options.ManufacturerId, data);
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAdvertiseBle(CancellationToken cancellationToken)
    {
        container.TryRemove(device);
        return ValueTask.CompletedTask;
    }
    #endregion

    #region Discovery
    ScanOptions? _scanOptions;
    public async ValueTask StartScanBle(ScanOptions scanOptions, CancellationToken cancellationToken = default)
    {
        _scanOptions = scanOptions;
        container.FoundDevice += OnFoundNewDevice;

        await cancellationToken.AwaitCancellation();
    }

    public ValueTask StopScanBle(CancellationToken cancellationToken)
    {
        container.FoundDevice -= OnFoundNewDevice;
        return ValueTask.CompletedTask;
    }

    void OnFoundNewDevice(DeviceContainer.Device device, DeviceContainer.Adverstisement ad)
    {
        if (_scanOptions is null)
            return;

        if (ad.Manufacturer != Constants.BLeBeaconManufacturerId)
            return;

        if (!BLeBeacon.TryParse(ad.Data.ToArray(), out var beaconData))
            return;

        _scanOptions.OnDeviceDiscovered?.Invoke(beaconData);
    }
    #endregion
}
