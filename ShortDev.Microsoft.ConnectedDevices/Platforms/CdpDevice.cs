using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms;

public record CdpDevice(string? Name, DeviceType Type, EndpointInfo Endpoint)
{
    public CdpDevice WithEndpoint(EndpointInfo endpoint)
        => new(Name, Type, endpoint);
}
