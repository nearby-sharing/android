using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices;

public record ConnectOptions
{
    public EventHandler<CdpTransportType>? TransportUpgraded { get; init; }
}
