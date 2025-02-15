using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;

namespace ShortDev.Microsoft.ConnectedDevices;
public static class Extensions
{
    public static uint HighValue(this ulong value)
        => (uint)(value >> 32);

    public static uint LowValue(this ulong value)
        => (uint)(value & uint.MaxValue);

    public static Task AwaitCancellation(this CancellationToken @this)
    {
        TaskCompletionSource promise = new();
        @this.Register(() => promise.TrySetResult());
        return promise.Task;
    }

    public static async Task<T?> AwaitWithTimeout<T>(this Task<T> task, TimeSpan timeout)
    {
        var timeoutTask = Task.Delay(timeout);
        if (task == await Task.WhenAny(task, timeoutTask).ConfigureAwait(false))
            return task.GetAwaiter().GetResult();

        Forget(task);
        throw new TaskCanceledException();

        static async void Forget(Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch { }
        }
    }

    public static string ToStringFormatted(this PhysicalAddress @this)
        => string.Join(':', Array.ConvertAll(@this.GetAddressBytes(), (x) => x.ToString("X2")));

    public static void DisposeAll(params IEnumerable<IDisposable>[] disposables)
        => disposables.SelectMany(x => x).DisposeAll();

    public static void DisposeAll<T>([NotNull] this IEnumerable<T> disposables) where T : IDisposable
    {
        List<Exception> exceptions = [];

        foreach (var disposable in disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
            throw new AggregateException(exceptions);
    }

    public readonly struct ArrayPoolToken<T>(ArrayPool<T> pool, int capacity) : IDisposable
    {
        private readonly ArrayPool<T>? _pool = pool;
        private readonly int _capacity = capacity;
        private readonly T[] _array = pool.Rent(capacity);

        public T[] ArrayUnsafe => _array;

        public Memory<T> Memory => _array.AsMemory()[0.._capacity];
        public Span<T> Span => Memory.Span;

        public void Dispose()
            => _pool?.Return(_array);
    }

    public static ArrayPoolToken<T> RentToken<T>(this ArrayPool<T> pool, int capacity)
        => new(pool, capacity);
}
