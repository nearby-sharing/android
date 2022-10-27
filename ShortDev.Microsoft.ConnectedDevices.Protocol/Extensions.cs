using System;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

public static class Extensions
{
    public static T[] Reverse<T>(this T[] source)
    {
        source = (T[])source.Clone();
        Array.Reverse(source);
        return source;
    }
}
