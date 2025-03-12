namespace ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;
public sealed class BluetoothTransport(IBluetoothHandler handler) : ICdpTransport, ICdpDiscoverableTransport
{
    readonly IBluetoothHandler _handler = handler;

    public CdpTransportType TransportType { get; } = CdpTransportType.Rfcomm;
    public bool IsEnabled => _handler.IsEnabled;

    #region Listen
    public event DeviceConnectedEventHandler? DeviceConnected;
    public ValueTask StartListen(CancellationToken cancellationToken)
    {
        return _handler.StartListenRfcomm(
            new RfcommOptions()
            {
                ServiceId = Constants.RfcommServiceId,
                ServiceName = Constants.RfcommServiceName,
                SocketConnected = (socket) => DeviceConnected?.Invoke(this, socket)
            },
            cancellationToken
        );
    }

    public ValueTask StopListen(CancellationToken cancellationToken)
        => _handler.StopListenRfcomm(cancellationToken);
    #endregion

    public async Task<CdpSocket> ConnectAsync(EndpointInfo endpoint, CancellationToken cancellationToken = default)
        => await _handler.ConnectRfcommAsync(endpoint, new RfcommOptions()
        {
            ServiceId = Constants.RfcommServiceId,
            ServiceName = Constants.RfcommServiceName,
            SocketConnected = (socket) => DeviceConnected?.Invoke(this, socket)
        }, cancellationToken).ConfigureAwait(false);

    #region Advertisement
    public ValueTask StartAdvertisement(LocalDeviceInfo deviceInfo, CancellationToken cancellationToken)
    {
        return _handler.StartAdvertiseBle(
            new AdvertiseOptions()
            {
                ManufacturerId = Constants.BLeBeaconManufacturerId,
                BeaconData = new BLeBeacon(deviceInfo.Type, _handler.MacAddress, deviceInfo.Name)
            },
            cancellationToken
        );
    }

    public ValueTask StopAdvertisement(CancellationToken cancellationToken)
        => _handler.StopAdvertiseBle(cancellationToken);
    #endregion

    #region Discovery
    public event DeviceDiscoveredEventHandler? DeviceDiscovered;

    public ValueTask StartDiscovery(CancellationToken cancellationToken)
    {
        return _handler.StartScanBle(new()
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

    public ValueTask StopDiscovery(CancellationToken cancellationToken)
        => _handler.StopScanBle(cancellationToken);
    #endregion

    public void Dispose()
    {
        DeviceConnected = null;
    }

    public EndpointInfo GetEndpoint()
        => new(TransportType, _handler.MacAddress.ToStringFormatted(), Constants.RfcommServiceId);
}
