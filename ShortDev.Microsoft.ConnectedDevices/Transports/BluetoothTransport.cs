using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Platforms.Bluetooth;
using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public sealed class BluetoothTransport : ICdpTransport, ICdpDiscoverableTransport
{
    public CdpTransportType TransportType { get; } = CdpTransportType.Rfcomm;

    public IBluetoothHandler Handler { get; }
    public BluetoothTransport(IBluetoothHandler handler)
    {
        Handler = handler;
    }

    public event DeviceConnectedEventHandler? DeviceConnected;
    public void Listen(CancellationToken cancellationToken)
    {
        _ = Handler.ListenRfcommAsync(
            new RfcommOptions()
            {
                ServiceId = Constants.RfcommServiceId,
                ServiceName = Constants.RfcommServiceName,
                SocketConnected = (socket) => DeviceConnected?.Invoke(this, socket)
            },
            cancellationToken
        );
    }

    public bool PreferRfcomm { get; set; } = false;
    public async Task<CdpSocket> ConnectAsync(CdpDevice device)
    {
        if (Handler.SupportsRfcomm && PreferRfcomm)
            return await Handler.ConnectRfcommAsync(device, new RfcommOptions()
            {
                ServiceId = Constants.RfcommServiceId,
                ServiceName = Constants.RfcommServiceName,
                SocketConnected = (socket) => DeviceConnected?.Invoke(this, socket)
            });

        return await Handler.ConnectGattAsync(device, new()
        {
            SocketConnected = (socket) => DeviceConnected?.Invoke(this, socket)
        });
    }

    public void Advertise(CdpAdvertisement options, CancellationToken cancellationToken)
    {
        _ = Handler.AdvertiseBLeBeaconAsync(
            new AdvertiseOptions()
            {
                ManufacturerId = Constants.BLeBeaconManufacturerId,
                BeaconData = options.GenerateBLeBeacon(),
                GattServiceId = $"BAD956B2-900A-45AD-B42F-{options.MacAddress}"
            },
            cancellationToken
        );
    }

    public event DeviceDiscoveredEventHandler? DeviceDiscovered;
    public void Discover(CancellationToken cancellationToken)
    {
        _ = Handler.ScanBLeAsync(new()
        {
            OnDeviceDiscovered = (advertisement) =>
            {
                CdpDevice device = new(
                    advertisement.DeviceName,
                    TransportType,
                    advertisement.MacAddress.ToStringFormatted()
                );
                DeviceDiscovered?.Invoke(this, device, advertisement);
            }
        }, cancellationToken);
    }

    public void Dispose()
    {
        DeviceConnected = null;
    }
}
