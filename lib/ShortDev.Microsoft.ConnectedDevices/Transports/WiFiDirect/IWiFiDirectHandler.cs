using System.Net;
using System.Net.NetworkInformation;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;
public interface IWiFiDirectHandler : IDisposable
{
    Task<IPAddress> ConnectAsync(EndpointInfo device, CancellationToken cancellationToken = default);

    PhysicalAddress MacAddress { get; }
    bool IsEnabled { get; }
}
