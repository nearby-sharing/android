using Android.Net.Wifi.P2p;
using Android.Runtime;
using System.Runtime.CompilerServices;
using static Android.Net.Wifi.P2p.WifiP2pManager;

namespace NearShare.Android.WiFiDirect;

sealed class ActionListener : Java.Lang.Object, IActionListener
{
    readonly TaskCompletionSource _promise = new();
    public void OnFailure([GeneratedEnum] WifiP2pFailureReason reason)
        => _promise.SetException(new Exception(reason.ToString()));

    public void OnSuccess()
        => _promise.SetResult();

    public TaskAwaiter GetAwaiter()
        => _promise.Task.GetAwaiter();
}
