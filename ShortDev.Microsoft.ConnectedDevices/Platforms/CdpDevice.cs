using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms;

public record CdpDevice(string Name, CdpTransportType TransportType, string Address)
{
}
