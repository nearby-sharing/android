using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace ShortDev.Microsoft.ConnectedDevices;

public sealed class BasicLoggingProvider : ILoggerProvider
{
    readonly ConcurrentDictionary<string, ILogger> _loggerCache = new();
    ILogger ILoggerProvider.CreateLogger(string categoryName)
        => _loggerCache.GetOrAdd(categoryName, _ => new BasicLogger()
        {
            Provider = this,
            CategoryName = categoryName
        });

    void IDisposable.Dispose() { }

    public event Action<string>? MessageReceived;

    sealed class BasicLogger : ILogger
    {
        public required BasicLoggingProvider Provider { get; init; }
        public required string CategoryName { get; init; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Provider.MessageReceived?.Invoke(formatter(state, exception));
        }
    }
}
