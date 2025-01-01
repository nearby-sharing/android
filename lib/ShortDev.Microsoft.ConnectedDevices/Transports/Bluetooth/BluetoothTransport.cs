namespace ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;
public sealed class BluetoothTransport(IBluetoothHandler handler) : ICdpTransport, ICdpDiscoverableTransport
{
    readonly IBluetoothHandler _handler = handler;

    public CdpTransportType TransportType { get; } = CdpTransportType.Rfcomm;
    public bool IsEnabled => _handler.IsEnabled;

    public event DeviceConnectedEventHandler? DeviceConnected;
    public async Task Listen(CancellationToken cancellationToken)
    {
        await _handler.ListenRfcommAsync(
            new RfcommOptions()
            {
                ServiceId = Constants.RfcommServiceId,
                ServiceName = Constants.RfcommServiceName,
                SocketConnected = (socket) => DeviceConnected?.Invoke(this, socket)
            },
            cancellationToken
        );
    }

    public async Task<CdpSocket> ConnectAsync(EndpointInfo endpoint, CancellationToken cancellationToken = default)
        => await _handler.ConnectRfcommAsync(endpoint, new RfcommOptions()
        {
            ServiceId = Constants.RfcommServiceId,
            ServiceName = Constants.RfcommServiceName,
            SocketConnected = (socket) => DeviceConnected?.Invoke(this, socket)
        }, cancellationToken);

    public async Task Advertise(LocalDeviceInfo deviceInfo, CancellationToken cancellationToken)
    {
        await _handler.AdvertiseBLeBeaconAsync(
            new AdvertiseOptions()
            {
                ManufacturerId = Constants.BLeBeaconManufacturerId,
                BeaconData = new BLeBeacon(deviceInfo.Type, _handler.MacAddress, deviceInfo.Name)
            },
            cancellationToken
        );
    }

    public event DeviceDiscoveredEventHandler? DeviceDiscovered;
    public async Task Discover(CancellationToken cancellationToken)
    {
        await _handler.ScanBLeAsync(new()
        {
            OnDeviceDiscovered = (advertisement, rssi) =>
            {
                CdpDevice device = new(
                    advertisement.DeviceName,
                    advertisement.DeviceType,
                    EndpointInfo.FromRfcommDevice(advertisement.MacAddress)
                )
                {
                    Rssi = rssi
                };
                DeviceDiscovered?.Invoke(this, device);
            }
        }, cancellationToken);
    }

    public void Dispose()
    {
        DeviceConnected = null;
    }

    public EndpointInfo GetEndpoint()
        => new(TransportType, _handler.MacAddress.ToStringFormatted(), Constants.RfcommServiceId);
}
