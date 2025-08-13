using AndroidX.Lifecycle;

namespace NearShare.Utils;

public static class LifecycleExtensions
{
    extension(ILifecycleOwner owner)
    {
        public bool IsAtLeastStarted => owner.Lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Started!);
    }
}

