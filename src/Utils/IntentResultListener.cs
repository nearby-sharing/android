using Android.Content;
using Android.Runtime;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;

namespace NearShare.Utils;

public sealed class IntentResultListener<TActivity> where TActivity : Activity, IActivityResultCaller
{
    readonly ResultCallback _callback;
    readonly ActivityResultLauncher _launcher;
    public IntentResultListener(TActivity activity)
    {
        _launcher = activity.RegisterForActivityResult(
            new ActivityResultContracts.StartActivityForResult(),
            _callback = new()
        );
    }

    public async Task<ActivityResult> LaunchAsync(Intent intent)
    {
        TaskCompletionSource<ActivityResult> promise = new();
        _callback.ResultReceived += OnResultReceived;

        _launcher.Launch(intent);

        return await promise.Task;

        void OnResultReceived(object? sender, ActivityResult result)
        {
            _callback.ResultReceived -= OnResultReceived;
            promise.TrySetResult(result);
        }
    }

    sealed class ResultCallback : Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Java.Lang.Object? result)
        {
            if (result is null)
                return;

            ResultReceived?.Invoke(this, result.JavaCast<ActivityResult>());
        }

        public event EventHandler<ActivityResult>? ResultReceived;
    }
}
