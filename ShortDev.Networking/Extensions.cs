using System;
using System.Runtime.InteropServices;

namespace ShortDev.Networking;

public static class Extensions
{
    public static Span<T> AsSpan<T>(this ReadOnlySpan<T> buffer)
        => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(buffer), buffer.Length);
}
