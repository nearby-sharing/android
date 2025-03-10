namespace ShortDev.Microsoft.ConnectedDevices.Transports;
public interface ICdpDiscoverableTransport : ICdpTransport
{
    ValueTask StartAdvertisement(LocalDeviceInfo deviceInfo, CancellationToken cancellationToken);
    ValueTask StopAdvertisement(CancellationToken cancellationToken);

    event DeviceDiscoveredEventHandler? DeviceDiscovered;
    ValueTask StartDiscovery(CancellationToken cancellationToken);
    ValueTask StopDiscovery(CancellationToken cancellationToken);
}
