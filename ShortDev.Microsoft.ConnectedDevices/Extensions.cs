using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices;

public static class Extensions
{
    public static uint HighValue(this ulong value)
        => (uint)(value >> 32);

    public static uint LowValue(this ulong value)
        => (uint)(value & uint.MaxValue);

    public static Task AwaitCancellation(this CancellationToken @this)
    {
        TaskCompletionSource<bool> promise = new();
        @this.Register(() => promise.SetResult(true));
        return promise.Task;
    }

    public static async Task<T?> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
    {
        if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            return task.Result;
        return default;
    }

    public static ECDsa ToECDsa(this ECDiffieHellman @this)
    {
        ECDsa result = ECDsa.Create();
        result.ImportParameters(@this.ExportParameters(true));
        return result;
    }

    public static string ToStringFormatted(this PhysicalAddress @this)
        => string.Join(':', Array.ConvertAll(@this.GetAddressBytes(), (x) => x.ToString("X2")));

    public static void DisposeAll(params IEnumerable<IDisposable>[] disposables)
        => disposables.SelectMany(x => x).DisposeAll();

    public static void DisposeAll<T>(this IEnumerable<T> disposables) where T : IDisposable
    {
        List<Exception> exceptions = new();

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

    public readonly struct ArrayPoolToken<T> : IDisposable
    {
        private readonly ArrayPool<T>? _pool;
        private readonly T[] _array;
        private readonly int _capacity;

        private ArrayPoolToken(ArrayPool<T> pool, int capacity)
        {
            _pool = pool;
            _capacity = capacity;
            _array = pool.Rent(capacity);
        }

        public static ArrayPoolToken<T> Create(ArrayPool<T> pool, int capacity)
            => new(pool, capacity);

        public Memory<T> Memory => _array.AsMemory()[0.._capacity];
        public Span<T> Span => Memory.Span;

        public void Dispose()
            => _pool?.Return(_array);
    }

    public static ArrayPoolToken<T> RentToken<T>(this ArrayPool<T> pool, int capacity)
        => ArrayPoolToken<T>.Create(pool, capacity);
}
