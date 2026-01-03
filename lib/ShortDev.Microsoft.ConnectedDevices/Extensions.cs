using ShortDev.IO.Buffers;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

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
    }

    public static async void Forget(this Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch { }
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

    public static ReadOnlyMemory<byte> ReadBytesWithLength<TReader>(this ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        var length = reader.ReadUInt16();
        byte[] buffer = new byte[length];
        reader.ReadBytes(buffer);
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static X509Certificate2 ReadCert<TReader>(this ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        var length = reader.ReadUInt16();

        using var buffer = ArrayPool<byte>.Shared.RentMemory(length);
        reader.ReadBytes(buffer.Span);

        return X509CertificateLoader.LoadCertificate(buffer.Span);
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PhysicalAddress ReadPhysicalAddress<TReader>(this ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        byte[] buffer = new byte[6];
        reader.ReadBytes(buffer);
        buffer.AsSpan().Reverse();
        return new PhysicalAddress(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<TWriter>(this ref TWriter writer, PhysicalAddress address) where TWriter : struct, IEndianWriter, allows ref struct
    {
        var bytes = address.GetAddressBytes();
        bytes.AsSpan().Reverse();
        writer.Write(bytes);
    }
}
