using Android.Content;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using System.Collections.Concurrent;

namespace NearShare.Utils;

[Activity(Exported = false)]
internal sealed class DelegateActivity : ComponentActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var args = savedInstanceState ??= Intent?.Extras;
        if (args?.ContainsKey("id") != true)
        {
            Finish();
            return;
        }

        var id = args.GetInt("id");
        var (intent, promise) = _pendingResults[id];

        ActivityResultCallerKt.RegisterForActivityResult(
            this,
            new ActivityResultContracts.StartActivityForResult(),
            intent,
            (KtAction<ActivityResult>)(result =>
            {
                Finish();
                switch (result.ResultCode)
                {
                    case (int)Result.Ok:
                        promise.SetResult(result.Data);
                        break;

                    case (int)Result.Canceled:
                        promise.SetCanceled();
                        break;

                    case var errorCode:
                        promise.SetException(new InvalidOperationException($"Activity finished with error code: {errorCode}"));
                        break;
                }
            })
        ).Launch(Kotlin.Unit.Instance);
    }

    static readonly ConcurrentDictionary<int, (Intent intent, TaskCompletionSource<Intent?> promise)> _pendingResults = [];
    public static Task<Intent?> Launch(Context context, Intent intent)
    {
        TaskCompletionSource<Intent?> promise = new();

        var id = promise.GetHashCode();
        if (!_pendingResults.TryAdd(id, (intent, promise)))
            throw new InvalidOperationException("Failed to register task.");

        Intent delegateIntent = new(context, typeof(DelegateActivity));
        delegateIntent.PutExtra("id", id);
        delegateIntent.SetFlags(ActivityFlags.ReceiverForeground);
        context.StartActivity(delegateIntent);

        return promise.Task;
    }
}
