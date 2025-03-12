using System.Runtime.CompilerServices;

namespace ShortDev.Microsoft.ConnectedDevices;

public sealed class BackgroundAction(Task task, CancellationTokenSource stoppingCancellation)
{
    public async ValueTask StopAsync(CancellationToken cancellation)
    {
        try
        {
            await stoppingCancellation.CancelAsync();
            stoppingCancellation.Dispose();
        }
        finally
        {
            await task.WaitAsync(cancellation);
        }
    }

    public static BackgroundAction Start(Func<CancellationToken, Task> executeAction, CancellationToken cancellation)
    {
        var stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);

        return new(executeAction(stoppingCts.Token), stoppingCts);
    }

    public static ValueTask Start(ref BackgroundAction? action, Func<CancellationToken, Task> executeAction, CancellationToken cancellation, [CallerArgumentExpression(nameof(action))] string expression = "")
    {
        if (action is not null)
            throw new InvalidOperationException($"Action '{expression}' is already running");

        action = Start(executeAction, cancellation);
        return ValueTask.CompletedTask;
    }

    public static ValueTask Stop(ref BackgroundAction? action, CancellationToken cancellation, [CallerArgumentExpression(nameof(action))] string expression = "")
    {
        if (action is null)
            throw new InvalidOperationException($"Action '{expression}' is not running");

        var instance = action;
        action = null;

        return instance.StopAsync(cancellation);
    }
}
