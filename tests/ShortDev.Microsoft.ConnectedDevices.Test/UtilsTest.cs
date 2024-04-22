using System.Diagnostics;

namespace ShortDev.Microsoft.ConnectedDevices.Test;
public sealed class UtilsTest
{
    [Theory]
    [InlineData(200, 1)]
    [InlineData(200, 10)]
    [InlineData(200, 100)]
    public async Task WithTimeout_ShouldObserveException_WhenSlowerTimeout(int delayMs, int timeoutMs)
    {
        using UnobservedTaskExceptionObserver exceptionObserver = new();

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
        using UnobservedTaskExceptionObserver exceptionObserver = new();

        await Assert.ThrowsAsync<NotImplementedException>(async () =>
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
        throw new NotImplementedException();
    }

    sealed class UnobservedTaskExceptionObserver : IDisposable
    {
        public UnobservedTaskExceptionObserver()
            => TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        bool _hadUnobservedTaskException;
        void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
            => _hadUnobservedTaskException = true;

        [StackTraceHidden]
        public void Dispose()
        {
            // Force GC to cleanup long-running task
            GC.Collect();
            GC.WaitForPendingFinalizers();

            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

            CheckForUnobservedTaskExceptions();
        }

        void CheckForUnobservedTaskExceptions()
            => Assert.False(_hadUnobservedTaskException);
    }
}
