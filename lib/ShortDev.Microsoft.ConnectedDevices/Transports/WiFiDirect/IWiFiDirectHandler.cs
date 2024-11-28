using System.Net;
using System.Net.NetworkInformation;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;
public interface IWiFiDirectHandler : IDisposable
{
    Task<IPAddress> ConnectAsync(string address, string ssid, string passphrase, CancellationToken cancellationToken = default);
    Task CreateGroupAutonomous(string ssid, string passphrase);

    PhysicalAddress MacAddress { get; }
    bool IsEnabled { get; }
}
