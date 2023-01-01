using System.Threading;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Transports;

public interface ICdpDiscoverableTransport : ICdpTransport
{
    void Advertise(CdpAdvertisement options, CancellationToken cancellationToken);
    event DeviceDiscoveredEventHandler? DeviceDiscovered;
    void Discover(CancellationToken cancellationToken);
}
