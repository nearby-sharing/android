namespace ShortDev.Microsoft.ConnectedDevices;

public sealed class GuardFlag
{
    int _instanceCount = 0;
    CancellationTokenSource _cancellationSource = new();
    public IDisposable Run<T>(Func<T, CancellationToken, ValueTask> action, T instance)
    {
        bool first = Interlocked.Increment(ref _instanceCount) == 0;

        if (!first)
        {
            cancellation.Register(() => Interlocked.Decrement(ref _instanceCount));
            return;
        }

        return new Reset()
        {
            OnRelease = OnRelease
        };
    }

    void OnRelease()
    {
        if (Interlocked.Decrement(ref _instanceCount) > 0)
            return;


    }

    public static implicit operator bool(GuardFlag flag)
        => flag._instanceCount > 0;
}

file sealed class Reset : IDisposable
{
    public required Action OnRelease;

    bool _wasDisposed;
    public void Dispose()
    {
        if (_wasDisposed)
            return;
        _wasDisposed = true;

        OnRelease();
    }
}
