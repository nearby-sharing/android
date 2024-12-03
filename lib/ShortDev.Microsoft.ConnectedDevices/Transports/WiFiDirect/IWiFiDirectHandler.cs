using System.Net;
using System.Net.NetworkInformation;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;
public interface IWiFiDirectHandler : IDisposable
{
    Task<IPAddress> ConnectAsync(string address, GroupInfo groupInfo, CancellationToken cancellationToken = default);
    Task<GroupInfo> CreateAutonomousGroup();

    PhysicalAddress MacAddress { get; }
    bool IsEnabled { get; }
}
