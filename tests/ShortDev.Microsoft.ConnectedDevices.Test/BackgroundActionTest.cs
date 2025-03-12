using System.Runtime.CompilerServices;

namespace ShortDev.Microsoft.ConnectedDevices.Test;

public class BackgroundActionTest
{
    [Fact]
    public async Task Lifecycle()
    {
        BackgroundAction? action = null;
        StrongBox<bool> isRunning = new();

        Assert.Null(action);
        Assert.False(isRunning.Value);

        await BackgroundAction.Start(ref action, token => LongRunningBackgroundTask(isRunning, token), CancellationToken.None);

        Assert.NotNull(action);
        Assert.True(isRunning.Value);

        await BackgroundAction.Stop(ref action, CancellationToken.None);

        Assert.Null(action);
        Assert.False(isRunning.Value);
    }

    [Fact]
    public async Task Start_ShouldThrow_WhenSyncException()
    {
        BackgroundAction? action = null;

        Assert.Null(action);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await BackgroundAction.Start(ref action, SyncThrowingBackgroundTask, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Stop_ShouldThrow_WhenAsyncException()
    {
        BackgroundAction? action = null;

        Assert.Null(action);
        await BackgroundAction.Start(ref action, AsyncThrowingBackgroundTask, CancellationToken.None);
        Assert.NotNull(action);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await BackgroundAction.Stop(ref action, CancellationToken.None)
        );
    }

    static async Task LongRunningBackgroundTask(StrongBox<bool> isRunning, CancellationToken cancellation)
    {
        isRunning.Value = true;
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken: CancellationToken.None);
            }
        }
        finally
        {
            isRunning.Value = false;
        }
    }

    static Task SyncThrowingBackgroundTask(CancellationToken cancellation)
    {
        throw new InvalidOperationException();
    }

    static async Task AsyncThrowingBackgroundTask(CancellationToken cancellation)
    {
        await Task.Delay(1000, cancellationToken: CancellationToken.None);
        throw new InvalidOperationException();
    }
}
