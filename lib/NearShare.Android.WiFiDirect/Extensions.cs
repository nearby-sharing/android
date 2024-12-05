using Android.Net.Wifi.P2p;
using System.Runtime.Versioning;

namespace NearShare.Android.WiFiDirect;

public static class Extensions
{
    public static async Task<WifiP2pGroup?> RequestGroupInfoAsync(this WifiP2pManager manager, WifiP2pManager.Channel channel)
    {
        GroupInfoPromise promise = new();
        manager.RequestGroupInfo(channel, promise);
        return await promise;
    }

    [SupportedOSPlatform("android29.0")]
    public static async Task StartAutonomousGroupAsync(this WifiP2pManager manager, WifiP2pManager.Channel channel, WifiP2pConfig config)
    {
        ActionListener listener = new();
        manager.CreateGroup(channel, config, listener);
        await listener;
    }
}
