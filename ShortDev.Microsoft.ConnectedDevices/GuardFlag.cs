using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ShortDev.Microsoft.ConnectedDevices;

public sealed class GuardFlag() : IDisposable
{
    int _value = 0;

    public IDisposable Lock([CallerMemberName] string methodName = "")
    {
        if (Interlocked.Exchange(ref _value, 1) == 1)
            throw new InvalidOperationException($"Method {methodName} is already running");

        return this;
    }

    void IDisposable.Dispose()
        => Interlocked.Exchange(ref _value, 0);

    public static implicit operator bool(GuardFlag flag)
        => flag._value == 1;
}
