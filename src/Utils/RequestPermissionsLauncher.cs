using Android.Content;
using Android.Runtime;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Core.Content;

namespace NearShare.Utils;

public sealed class RequestPermissionsLauncher
{
    readonly Context _context;
    readonly ActivityResultLauncher _launcher;
    readonly ResultCallback _callback;
    readonly string[] _permissions;
    public RequestPermissionsLauncher(IActivityResultCaller resultCaller, Context context, IReadOnlyList<string> permissions)
    {
        _context = context;
        _launcher = resultCaller.RegisterForActivityResult(
           new ActivityResultContracts.RequestMultiplePermissions(),
           _callback = new()
        );
        _permissions = [.. permissions];
    }

    public async Task<PermissionResult> RequestAsync()
    {
        if (_permissions.Length == 0)
            return PermissionResult.Granted.Instance;

        var deniedPermissions = _permissions
            .Where(x => ContextCompat.CheckSelfPermission(_context, x) == Android.Content.PM.Permission.Denied)
            .ToArray();

        if (deniedPermissions.Length == 0)
            return PermissionResult.Granted.Instance;

        TaskCompletionSource<PermissionResult> promise = new();
        _callback.ResultReceived += OnResultReceived;

        _launcher.Launch(_permissions);

        return await promise.Task;

        void OnResultReceived(object? sender, PermissionResult result)
        {
            _callback.ResultReceived -= OnResultReceived;
            promise.TrySetResult(result);
        }
    }

    sealed class ResultCallback : Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Java.Lang.Object? result)
        {
            var deniedPermissions = ToDictionary<string, bool>(result)?
                .Where(x => x.Value == false)
                .Select(x => x.Key)
                .ToArray() ?? [];

            ResultReceived?.Invoke(this, deniedPermissions switch
            {
                [] => PermissionResult.Granted.Instance,
                _ => new PermissionResult.Denied(deniedPermissions)
            });
        }

        private static IDictionary<TKey, TValue>? ToDictionary<TKey, TValue>(Java.Lang.Object? obj)
        {
            if (obj == null)
                return null;

            return JavaDictionary<TKey, TValue>.FromJniHandle(
                obj.PeerReference.NewLocalRef().Handle,
                JniHandleOwnership.TransferLocalRef
            );
        }

        public event EventHandler<PermissionResult>? ResultReceived;
    }
}

public closed record PermissionResult
{
    public sealed record Granted : PermissionResult
    {
        private Granted() { }

        public static Granted Instance { get; } = new Granted();
    }

    public sealed record Denied(IReadOnlyList<string> Permissions) : PermissionResult;
}
