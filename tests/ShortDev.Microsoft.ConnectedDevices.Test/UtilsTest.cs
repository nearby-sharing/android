using Xunit.Abstractions;

namespace ShortDev.Microsoft.ConnectedDevices.Test;
public sealed class UtilsTest(ITestOutputHelper output)
{
    [Theory]
    [InlineData(200, 1)]
    [InlineData(200, 10)]
    [InlineData(200, 100)]
    public async Task WithTimeout_ShouldObserveException_WhenSlowerTimeout(int delayMs, int timeoutMs)
    {
        using UnobservedTaskExceptionObserver exceptionObserver = new(output);

        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            // Await long-running task with shorter timeout
            await LongRunningOperationWithThrow(delayMs)
                .AwaitWithTimeout(TimeSpan.FromMilliseconds(timeoutMs));
        });

        // Wait for long-running task to complete
        await Task.Delay(delayMs * 2);
    }

    [Theory]
    [InlineData(1, 200)]
    [InlineData(10, 200)]
    [InlineData(100, 200)]
    public async Task WithTimeout_ShouldObserveException_WhenFasterAsTimeout(int delayMs, int timeoutMs)
    {
        using UnobservedTaskExceptionObserver exceptionObserver = new(output);

        await Assert.ThrowsAsync<ObservableException>(async () =>
        {
            // Await long-running task with longer timeout
            await LongRunningOperationWithThrow(delayMs)
                .AwaitWithTimeout(TimeSpan.FromMilliseconds(timeoutMs));
        });

        // Wait for long-running task to complete
        await Task.Delay(delayMs * 2);
    }

    static async Task<object> LongRunningOperationWithThrow(int delayMs)
    {
        await Task.Delay(delayMs);
        throw new ObservableException();
    }

    sealed class UnobservedTaskExceptionObserver : IDisposable
    {
        readonly ITestOutputHelper _output;
        public UnobservedTaskExceptionObserver(ITestOutputHelper output)
        {
            _output = output;

            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        int _unobservedExceptionCounter;
        void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            if (!e.Exception.InnerExceptions.All(x => x is ObservableException))
            {
                _output.WriteLine($"UnobservedTaskExceptions: {string.Join(',', e.Exception.InnerExceptions.Select(x => x.Message))}");
                return;
            }

            Interlocked.Increment(ref _unobservedExceptionCounter);
        }

        public void Dispose()
        {
            // Force GC to cleanup long-running task
            GC.Collect();
            GC.WaitForPendingFinalizers();

            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

            CheckForUnobservedTaskExceptions();
        }

        void CheckForUnobservedTaskExceptions()
            => Assert.Equal(0, _unobservedExceptionCounter);
    }

    sealed class ObservableException : Exception;
}
