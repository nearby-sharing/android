using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms.Bluetooth;
using System;
using System.Threading;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Transports;

public sealed class BluetoothTransport : ICdpTransport, ICdpDiscoverableTransport
{
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

    public CdpSocket Connect(CdpDevice device)
    {
        throw new NotImplementedException();
    }

    public void Advertise(CdpAdvertiseOptions options, CancellationToken cancellationToken)
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

    public void Dispose() { }
}
