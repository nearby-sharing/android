using ShortDev.Microsoft.ConnectedDevices.Transports;
using ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;
using System.IO.Pipes;
using System.Net.NetworkInformation;

namespace ShortDev.Microsoft.ConnectedDevices.Test.E2E;

internal sealed class BluetoothHandler(DeviceContainer container, DeviceContainer.Device device) : IBluetoothHandler
{
    public PhysicalAddress MacAddress => PhysicalAddress.Parse(device.Address);

    public Task<CdpSocket> ConnectRfcommAsync(EndpointInfo endpoint, RfcommOptions options, CancellationToken cancellationToken = default)
    {
        var device = container.FindDevice(endpoint.Address)
            ?? throw new KeyNotFoundException("Could not find device");

        return Task.FromResult(
            device.ConnectFrom(new(CdpTransportType.Rfcomm, device.Address, options.ServiceId ?? ""))
        );
    }

    public async Task ListenRfcommAsync(RfcommOptions options, CancellationToken cancellationToken = default)
    {
        device.ConnectionRequest += OnNewConnection;

        await cancellationToken.AwaitCancellation();

        device.ConnectionRequest -= OnNewConnection;

        void OnNewConnection(EndpointInfo client, ref (Stream Input, Stream Output)? clientStream)
        {
            AnonymousPipeServerStream serverInputStream = new(PipeDirection.In);
            AnonymousPipeServerStream serverOutputStream = new(PipeDirection.Out);

            // Accept connection
            clientStream = (
                new AnonymousPipeClientStream(PipeDirection.In, serverOutputStream.GetClientHandleAsString()),
                new AnonymousPipeClientStream(PipeDirection.Out, serverInputStream.GetClientHandleAsString())
            );

            options.SocketConnected?.Invoke(new CdpSocket()
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
    }

    public async Task AdvertiseBLeBeaconAsync(AdvertiseOptions options, CancellationToken cancellationToken = default)
    {
        var data = options.BeaconData.ToArray();
        container.Advertise(device, (uint)options.ManufacturerId, data);

        await cancellationToken.AwaitCancellation();

        container.TryRemove(device);
    }

    public async Task ScanBLeAsync(ScanOptions scanOptions, CancellationToken cancellationToken = default)
    {
        container.FoundDevice += OnNewDevice;

        await cancellationToken.AwaitCancellation();

        container.FoundDevice -= OnNewDevice;

        void OnNewDevice(DeviceContainer.Device device, DeviceContainer.Adverstisement ad)
        {
            if (ad.Manufacturer != Constants.BLeBeaconManufacturerId)
                return;

            if (!BLeBeacon.TryParse(ad.Data.ToArray(), out var beaconData))
                return;

            scanOptions.OnDeviceDiscovered?.Invoke(beaconData);
        }
    }
}
