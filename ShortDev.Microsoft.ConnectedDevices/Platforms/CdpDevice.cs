using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms;

public record CdpDevice(string? Name, EndpointInfo Endpoint)
{
    public CdpDevice WithEndpoint(EndpointInfo endpoint)
        => new(Name, endpoint);
}
