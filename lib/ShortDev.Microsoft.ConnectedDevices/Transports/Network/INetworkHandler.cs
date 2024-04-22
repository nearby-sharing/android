using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace ShortDev.Microsoft.ConnectedDevices.Transports.Network;

public interface INetworkHandler
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
        var ip = Dns.GetHostEntry(string.Empty).AddressList
            .Where((x) => x.AddressFamily == AddressFamily.InterNetwork)
            .FirstOrDefault();

        return ip ?? throw new InvalidDataException("Could not resolve ip");
    }
}
