using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace ShortDev.Microsoft.ConnectedDevices.Messages.Connection.TransportUpgrade;

public record class HostEndpoint(CdpTransportType Type, string Host, string Service)
{
    public static HostEndpoint FromIP(string ip)
        => new(CdpTransportType.Tcp, ip, Constants.TcpPort.ToString());
}
