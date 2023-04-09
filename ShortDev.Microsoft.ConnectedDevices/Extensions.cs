using System;
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
}
