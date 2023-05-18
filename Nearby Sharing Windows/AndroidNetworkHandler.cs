using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using AndroidX.Core.Net;
using ShortDev.Microsoft.ConnectedDevices.Platforms;
using ShortDev.Microsoft.ConnectedDevices.Platforms.Network;
using System.Net;

namespace Nearby_Sharing_Windows;

internal sealed class AndroidNetworkHandler : INetworkHandler
{
    public Context Context { get; }
    public ICdpPlatformHandler PlatformHandler { get; }

    public AndroidNetworkHandler(ICdpPlatformHandler handler, Context context)
    {
        PlatformHandler = handler;
        Context = context;
    }

    public IPAddress GetLocalIp()
        => GetLocalIp(Context);

    public static IPAddress GetLocalIp(Context context)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            var connectivityManager = (ConnectivityManager?)context.GetSystemService(Context.ConnectivityService) ?? throw new InvalidOperationException($"Could not get {nameof(ConnectivityManager)}");
            var network = connectivityManager.ActiveNetwork ?? throw new InvalidOperationException($"Could not get active network");
            var linkProps = connectivityManager.GetLinkProperties(network) ?? throw new InvalidOperationException($"Could not get {nameof(LinkProperties)}");
            var address = linkProps.LinkAddresses
                .Select(x => x.Address?.GetAddress())
                .FirstOrDefault(x => x?.Length == 4) ?? throw new InvalidOperationException("Could not get ip v4 address");

            return new IPAddress(address);
        }

        WifiManager wifiManager = (WifiManager)context.GetSystemService(Context.WifiService)!;
        WifiInfo wifiInfo = wifiManager.ConnectionInfo!;
        int ip = wifiInfo.IpAddress;
        return new IPAddress(ip);
    }
}
