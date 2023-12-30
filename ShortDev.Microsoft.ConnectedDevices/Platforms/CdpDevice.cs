using ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms;

public record CdpDevice(string Name, DeviceType Type, EndpointInfo Endpoint)
{
    public double Rssi { get; init; } = double.NegativeInfinity;

    public CdpDevice WithEndpoint(EndpointInfo endpoint)
        => new(Name, Type, endpoint);

    public override int GetHashCode()
        => Endpoint.GetHashCode();

    public virtual bool Equals(CdpDevice? other)
        => other?.Endpoint.Equals(Endpoint) ?? false;
}
