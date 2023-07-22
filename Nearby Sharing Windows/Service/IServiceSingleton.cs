using Android.Content;

namespace Nearby_Sharing_Windows.Service;

internal interface IServiceSingleton<T> where T : Android.App.Service, IServiceSingleton<T>
{
    public static event StateChangedDelegate? StateChanged;
    delegate void StateChangedDelegate(T? service, bool isActive);

    public static void CallEvent(StateChangedDelegate? @delegate)
        => @delegate?.Invoke(Instance, IsActive);

    public static bool IsActive => Instance != null;
    public static T? Instance { get; private set; }
    void OnInstanceChanged(T? value)
    {
        Instance = value;
        CallEvent(StateChanged);
    }

    public static async ValueTask<T> EnsureRunning(Context context)
    {
        if (Instance != null)
            return Instance;

        context.StartService(new Intent(context, typeof(T)));
        await AwaitActivation();

        return Instance ?? throw new InvalidOperationException($"Could not get service instance");
    }

    static async ValueTask AwaitActivation()
    {
        if (Instance != null)
            return;

        TaskCompletionSource promise = new();

        StateChanged += OnStateChanged;
        void OnStateChanged(T? service, bool isActive)
        {
            if (!isActive)
                promise.TrySetCanceled();
            else
                promise.TrySetResult();

            StateChanged -= OnStateChanged;
        }

        await promise.Task;
    }
}
