using ShortDev.Networking;
using System;
using System.Diagnostics;
using System.IO;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

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
}
