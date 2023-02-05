using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

public record class HostEndpointMetadata(CdpTransportType Type, string Host, string Service)
{
    public static HostEndpointMetadata FromIP(string ip)
        => new(CdpTransportType.Tcp, ip, Constants.TcpPort.ToString());
}
