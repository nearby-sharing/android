using Android.Content;
using Android.OS;

namespace Nearby_Sharing_Windows.Service;

internal sealed class CdpServiceConnection : Java.Lang.Object, IServiceConnection
{
    readonly TaskCompletionSource<CdpService> _promise = new();
    public void OnServiceConnected(ComponentName? name, IBinder? service)
    {
        if (service is CdpServiceBinder { Service: CdpService result })
            _promise.SetResult(result);
    }

    public void OnServiceDisconnected(ComponentName? name) { }

    public static async Task<CdpService> ConnectToServiceAsync(Activity activity)
    {
        CdpServiceConnection serviceConnection = new();

        Intent intent = new(activity, typeof(CdpService));
        activity.BindService(intent, serviceConnection, Bind.None);

        return await serviceConnection._promise.Task;
    }
}
