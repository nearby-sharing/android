namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Connection.TransportUpgrade;

public record class HostEndpointMetadata(CdpTransportType Type, string Host, string Service)
{
    public static HostEndpointMetadata FromIP(string ip)
        => new HostEndpointMetadata(CdpTransportType.Tcp, ip, Constants.TcpPort.ToString());
}
