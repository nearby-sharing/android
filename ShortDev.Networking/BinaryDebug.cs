using System;
using System.Diagnostics;

namespace ShortDev.Networking;

public static class BinaryDebug
{
    [Conditional("DEBUG")]
    public static void PrintContent(ref EndianReader reader)
        => PrintContent(reader.Buffer.AsSpan());

    [Conditional("DEBUG")]
    public static void PrintContent(ReadOnlySpan<byte> content)
    {
        var hex = BitConverter.ToString(content.ToArray()).Replace("-", null);
        Debug.Print(hex);
    }
}
