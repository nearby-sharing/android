using Microsoft.Extensions.Logging;

namespace ShortDev.Microsoft.ConnectedDevices.Test.E2E;

internal sealed class TestLoggerProvider(string deviceName, ITestOutputHelper outputHelper) : ILoggerProvider, ILogger
{
    public ILogger CreateLogger(string categoryName)
        => this;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel)
        => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var msg = formatter(state, exception);
        if (exception is not null)
            msg += '\n' + exception.Message;

        outputHelper.WriteLine($"[{logLevel}]: [{deviceName}]: ({eventId.Name}) {msg}");
    }

    public void Dispose() { }
}
