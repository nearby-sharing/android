using Android.Runtime;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Java.Util;
using System.Diagnostics;
using Activity = Android.App.Activity;

namespace NearShare.Utils;

public sealed class RequestPermissionsLauncher
{
    readonly Activity _activity;
    readonly ActivityResultLauncher _launcher;
    readonly ResultCallback _callback;
    readonly string[] _permissions;
    public RequestPermissionsLauncher(IActivityResultCaller resultCaller, Activity activity, IReadOnlyList<string> permissions)
    {
        _activity = activity;
        _launcher = resultCaller.RegisterForActivityResult(
           new ActivityResultContracts.RequestMultiplePermissions(),
           _callback = new()
        );
        _permissions = [.. permissions];
    }

    public async Task<PermissionResult> RequestAsync()
    {
        var deniedPermissions = _permissions
            .Where(x => ContextCompat.CheckSelfPermission(_activity, x) == Android.Content.PM.Permission.Denied)
            .ToArray();

        if (deniedPermissions.Length == 0)
            return PermissionResult.Granted.Instance;

        var showRationalFor = deniedPermissions
            .Where(x => ActivityCompat.ShouldShowRequestPermissionRationale(_activity, x))
            .ToArray();

        //if (showRationalFor.Length != 0)
        //    return new PermissionResult.Denied(showRationalFor);

        TaskCompletionSource<PermissionResult> promise = new();
        _callback.ResultReceived += OnResultReceived;

        _launcher.Launch(_permissions);

        return await promise.Task;

        void OnResultReceived(object? sender, RequestPermissionResult result)
        {
            _callback.ResultReceived -= OnResultReceived;
            promise.TrySetResult(result switch
            {
                RequestPermissionResult.Denied(var denied) => new PermissionResult.Denied(denied),
                _ => PermissionResult.Granted.Instance,
            });
        }
    }

    sealed class ResultCallback : Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Java.Lang.Object? result)
        {
            if (result is not IMap map)
                throw new InvalidOperationException("Expected result to be of type IMap");

            List<string> deniedPermissions = [];
            foreach (Java.Lang.Object? entryObj in map.EntrySet())
            {
                var entry = entryObj.JavaCast<IMapEntry>();
                if (entry is null)
                    continue;

                var permission = (string?)entry.Key;
                var grantedValue = entry.Value;
                if (grantedValue is null || (bool)grantedValue is true)
                    continue;

                deniedPermissions.Add(
                    permission ?? throw new UnreachableException("Permission was null")
                );
            }

            ResultReceived?.Invoke(this, deniedPermissions switch
            {
                [] => RequestPermissionResult.Granted.Instance,
                _ => new RequestPermissionResult.Denied(deniedPermissions)
            });
        }

        public event EventHandler<RequestPermissionResult>? ResultReceived;
    }
}

public abstract record PermissionResult
{
    private PermissionResult() { }

    public sealed record Granted : PermissionResult
    {
        private Granted() { }

        public static Granted Instance { get; } = new Granted();
    }

    public sealed record Denied(IReadOnlyList<string> Permissions) : PermissionResult;
}

public abstract record RequestPermissionResult
{
    private RequestPermissionResult() { }

    public sealed record Granted : RequestPermissionResult
    {
        private Granted() { }

        public static Granted Instance { get; } = new Granted();
    }

    public sealed record Denied(IReadOnlyList<string> Permissions) : RequestPermissionResult;
}
