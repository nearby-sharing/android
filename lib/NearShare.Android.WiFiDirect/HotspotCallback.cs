using Android.Net.Wifi;
using Android.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace NearShare.Android.WiFiDirect;

[SupportedOSPlatform("android26.0")]
internal sealed class HotspotCallback : WifiManager.LocalOnlyHotspotCallback
{
    readonly TaskCompletionSource<WifiManager.LocalOnlyHotspotReservation?> _promise = new();
    public override void OnStarted(WifiManager.LocalOnlyHotspotReservation? reservation)
        => _promise.TrySetResult(reservation);

    public override void OnFailed([GeneratedEnum] LocalOnlyHotspotCallbackErrorCode reason)
        => _promise.TrySetException(new Exception(reason.ToString()));

    public TaskAwaiter<WifiManager.LocalOnlyHotspotReservation?> GetAwaiter()
        => _promise.Task.GetAwaiter();
}
