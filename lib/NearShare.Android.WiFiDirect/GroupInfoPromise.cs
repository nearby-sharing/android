using Android.Net.Wifi.P2p;
using System.Runtime.CompilerServices;

namespace NearShare.Android.WiFiDirect;

public sealed class GroupInfoPromise : Java.Lang.Object, WifiP2pManager.IGroupInfoListener
{
    readonly TaskCompletionSource<WifiP2pGroup?> _promise = new();

    public void OnGroupInfoAvailable(WifiP2pGroup? group)
        => _promise.TrySetResult(group);

    public TaskAwaiter<WifiP2pGroup?> GetAwaiter()
        => _promise.Task.GetAwaiter();
}
