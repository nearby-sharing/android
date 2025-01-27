using System.Diagnostics;

namespace ShortDev.Microsoft.ConnectedDevices;

public ref struct SpeedMeassure(uint length) : IDisposable
{
    readonly long _start = Stopwatch.GetTimestamp();

    public readonly void Dispose()
    {
        var deltaTime = Stopwatch.GetElapsedTime(_start);
        Debug.Print($"Speed: {Math.Round(length / 1_000.0 / deltaTime.TotalSeconds)} kByte/s; dT: {deltaTime}");
    }
}
