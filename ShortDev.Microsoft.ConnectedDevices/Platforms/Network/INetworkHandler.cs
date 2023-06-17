using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace ShortDev.Microsoft.ConnectedDevices.Platforms.Network;

public interface INetworkHandler : ICdpPlatformHandler
{
    IPAddress GetLocalIp();

    public IPAddress? TryGetLocalIp()
    {
        try
        {
            return GetLocalIp();
        }
        catch
        {
            return null;
        }
    }

    public static IPAddress GetLocalIpDefault()
    {
        var data = Dns.GetHostEntry(string.Empty).AddressList;
        var ips = Dns.GetHostEntry(string.Empty).AddressList
            .Where((x) => x.AddressFamily == AddressFamily.InterNetwork)
            .ToArray();
        if (ips.Length != 1)
            throw new InvalidDataException("Could not resolve ip");

        return ips[0];
    }
}
