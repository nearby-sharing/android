using ShortDev.Networking;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ShortDev.Microsoft.ConnectedDevices;

public static class Extensions
{
    public static T[] Reverse<T>(this T[] source)
    {
        source = (T[])source.Clone();
        Array.Reverse(source);
        return source;
    }

    public static byte[] ReadPayload(this BinaryReader @this)
    {
        var stream = @this.BaseStream;
        return @this.ReadBytes((int)(stream.Length - stream.Position));
    }

    public static void PrintPayload(this BinaryReader @this)
        => Debug.Print(BinaryConvert.ToString(@this.ReadPayload()));

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

    public static ECDsa ToECDsa(this ECDiffieHellman @this)
    {
        ECDsa result = ECDsa.Create();
        result.ImportParameters(@this.ExportParameters(true));
        return result;
    }

    public static string ToStringFormatted(this PhysicalAddress @this)
        => string.Join(':', Array.ConvertAll(@this.GetAddressBytes(), (x) => x.ToString("X2")));
}
