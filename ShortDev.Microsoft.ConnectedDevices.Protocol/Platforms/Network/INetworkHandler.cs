using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Platforms.Network;

public interface INetworkHandler : ICdpPlatformHandler
{
    string GetLocalIp();

    public static string GetLocalIpDefault()
    {
        var data = Dns.GetHostEntry(string.Empty).AddressList;
        var ips = Dns.GetHostEntry(string.Empty).AddressList
            .Where((x) => x.AddressFamily == AddressFamily.InterNetwork)
            .ToArray();
        if (ips.Length != 1)
            throw new InvalidDataException("Could not resolve ip");

        return ips[0].ToString();
    }
}
