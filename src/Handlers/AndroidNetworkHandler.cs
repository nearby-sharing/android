using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using ShortDev.Microsoft.ConnectedDevices.Transports.Network;
using System.Net;

namespace NearShare.Handlers;

internal sealed class AndroidNetworkHandler(Context context) : INetworkHandler
{
    public Context Context { get; } = context;

    public IPAddress GetLocalIp()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            var connectivityManager = (ConnectivityManager?)Context.GetSystemService(Context.ConnectivityService) ?? throw new InvalidOperationException($"Could not get {nameof(ConnectivityManager)}");
            var network = connectivityManager.ActiveNetwork ?? throw new InvalidOperationException($"Could not get active network");
            var linkProps = connectivityManager.GetLinkProperties(network) ?? throw new InvalidOperationException($"Could not get {nameof(LinkProperties)}");
            var address = linkProps.LinkAddresses
                .Select(x => x.Address?.GetAddress())
                .FirstOrDefault(x => x?.Length == 4) ?? throw new InvalidOperationException("Could not get ip v4 address");

            return new IPAddress(address);
        }

        WifiManager wifiManager = (WifiManager)Context.GetSystemService(Context.WifiService)!;
        WifiInfo wifiInfo = wifiManager.ConnectionInfo!;
        int ip = wifiInfo.IpAddress;
        return new IPAddress(ip);
    }
}
