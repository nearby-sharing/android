using System.Net;
using System.Net.NetworkInformation;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;
public interface IWiFiDirectHandler : IDisposable
{
    Task<IPAddress> ConnectAsync(string address, string ssid, ReadOnlyMemory<byte> passphrase, CancellationToken cancellationToken = default);
    Task CreateGroupAutonomous(string ssid, ReadOnlyMemory<byte> passphrase);

    PhysicalAddress MacAddress { get; }
    bool IsEnabled { get; }
}
