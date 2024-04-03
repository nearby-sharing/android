namespace ShortDev.Microsoft.ConnectedDevices.Transports;
public interface ICdpDiscoverableTransport : ICdpTransport
{
    Task Advertise(LocalDeviceInfo deviceInfo, CancellationToken cancellationToken);
    event DeviceDiscoveredEventHandler? DeviceDiscovered;
    Task Discover(CancellationToken cancellationToken);
}
