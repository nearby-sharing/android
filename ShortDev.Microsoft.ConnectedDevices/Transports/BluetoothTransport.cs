using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;
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

    public async Task<CdpSocket> ConnectAsync(CdpDevice device)
        => await Handler.ConnectRfcommAsync(device, new RfcommOptions()
        {
            ServiceId = Constants.RfcommServiceId,
            ServiceName = Constants.RfcommServiceName,
            SocketConnected = (socket) => DeviceConnected?.Invoke(this, socket)
        });

    public void Advertise(CdpAdvertisement options, CancellationToken cancellationToken)
    {
        _ = Handler.AdvertiseBLeBeaconAsync(
            new AdvertiseOptions()
            {
                ManufacturerId = Constants.BLeBeaconManufacturerId,
                BeaconData = options.GenerateBLeBeacon()
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
                    advertisement.DeviceType,
                    EndpointInfo.FromRfcommDevice(advertisement.MacAddress)
                );
                DeviceDiscovered?.Invoke(this, device, advertisement);
            }
        }, cancellationToken);
    }

    public void Dispose()
    {
        DeviceConnected = null;
    }

    public EndpointInfo GetEndpoint()
        => new(TransportType, Handler.MacAddress.ToStringFormatted(), Constants.RfcommServiceId);
}
