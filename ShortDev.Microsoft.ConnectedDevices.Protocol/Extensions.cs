using System;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol;

public static class Extensions
{
    public static T[] Reverse<T>(this T[] source)
    {
        Array.Reverse(source);
        return source;
    }
}
