using ShortDev.Microsoft.ConnectedDevices.Transports.Network;
using System.Net;

namespace ShortDev.Microsoft.ConnectedDevices.Test.E2E;

internal class NetworkHandler(IPAddress address) : INetworkHandler
{
    public IPAddress GetLocalIp() => address;
}
