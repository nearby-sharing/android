using Android.Content;
using Android.Net.Wifi;
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

    public string GetLocalIp()
        => GetLocalIp(Context);

    public static string GetLocalIp(Context context)
    {
        WifiManager wifiManager = (WifiManager)context.GetSystemService(Context.WifiService)!;
        WifiInfo wifiInfo = wifiManager.ConnectionInfo!;
        int ip = wifiInfo.IpAddress;
        return new IPAddress(ip).ToString();
    }

    public void Log(int level, string message)
        => PlatformHandler.Log(level, message);
}
