namespace NearShare.ViewModels;

internal static class Extensions
{
    public static void Post<T>(this SynchronizationContext? context, Action<T> action, T state)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (context is null)
        {
            action(state);
            return;
        }

        context.Post(static internalState =>
        {
            ArgumentNullException.ThrowIfNull(internalState);

            var (action, state) = ((Action<T>, T))internalState;
            action(state);

        }, (action, state));
    }

    public static void Post(this SynchronizationContext? context, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (context is null)
        {
            action();
            return;
        }
        context.Post(static internalState =>
        {
            ArgumentNullException.ThrowIfNull(internalState);
            var action = (Action)internalState;
            action();
        }, action);
    }
}
