using Android.Content.PM;
using AndroidX.Core.App;
using System.Collections.Concurrent;

namespace NearShare.Utils;

static class Intents
{
    internal const int requestCodeStart = 12000;

    static int requestCode = requestCodeStart;
    internal static int NextRequestCode()
    {
        if (Interlocked.Increment(ref requestCode) >= 12999)
            requestCode = requestCodeStart;

        return requestCode;
    }

    static readonly ConcurrentDictionary<int, TaskCompletionSource<Permission[]>> _permissionRequests = [];
    public static async Task<Permission[]> RequestPermissions(this Activity activity, params string[] permissions)
    {
        int requestCode = NextRequestCode();

        var promise = _permissionRequests.AddOrUpdate(
            requestCode,
            addValueFactory: static key => new(),
            updateValueFactory: static (key, old) =>
            {
                old.TrySetCanceled();
                return new();
            }
        );

        ActivityCompat.RequestPermissions(activity, permissions, requestCode);

        return await promise.Task;
    }

    public static void OnPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        if (!_permissionRequests.TryRemove(requestCode, out var promise))
            return;

        promise.TrySetResult(grantResults);
    }
}
