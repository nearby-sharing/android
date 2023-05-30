using ShortDev.Microsoft.ConnectedDevices.Platforms;
using System.Threading;

namespace ShortDev.Microsoft.ConnectedDevices.Transports;

public interface ICdpDiscoverableTransport : ICdpTransport
{
    void Advertise(LocalDeviceInfo deviceInfo, CancellationToken cancellationToken);
    event DeviceDiscoveredEventHandler? DeviceDiscovered;
    void Discover(CancellationToken cancellationToken);
}
