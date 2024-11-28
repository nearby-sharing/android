using ShortDev.Microsoft.ConnectedDevices.Transports;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;

namespace ShortDev.Microsoft.ConnectedDevices;

public record class EndpointInfo(
    [property: JsonPropertyName("endpointType")] CdpTransportType TransportType,
    [property: JsonPropertyName("host")] string Address,
    [property: JsonPropertyName("service")] string Service
)
{
    public IPEndPoint ToIPEndPoint()
    {
        if (!IPAddress.TryParse(Address, out var ipAddress))
            throw new InvalidOperationException($"Address \"{Address}\" is not a valid ip address");

        if (!int.TryParse(Service, out var port))
            throw new InvalidOperationException($"Service \"{Service}\" is not a valid port (integer)");

        return new(ipAddress, port);
    }

    public static EndpointInfo FromTcp(IPEndPoint endpoint)
        => FromTcp(endpoint.Address);

    public static EndpointInfo FromTcp(IPAddress address)
    => FromTcp(address.ToString());

    public static EndpointInfo FromTcp(string address)
        => new(CdpTransportType.Tcp, address, Constants.TcpPort.ToString());

    public static EndpointInfo FromWiFiDirect(PhysicalAddress macAddress)
        => new(CdpTransportType.WifiDirect, macAddress.ToStringFormatted(), "");

    public static EndpointInfo FromRfcommDevice(PhysicalAddress macAddress)
        => new(CdpTransportType.Rfcomm, macAddress.ToStringFormatted(), Constants.RfcommServiceId);
}
