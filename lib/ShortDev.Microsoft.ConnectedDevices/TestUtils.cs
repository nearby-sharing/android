using System.Net;

namespace ShortDev.Microsoft.ConnectedDevices;

internal sealed class TestUtils
{
    public static IPAddress ListenAddress { get; private set; } = IPAddress.Any;

    internal static void ListenLocalOnly()
    {
        ListenAddress = IPAddress.Loopback;
    }
}
