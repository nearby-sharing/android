using AndroidX.Lifecycle;
using System.ComponentModel;

namespace NearShare.Utils;

public sealed class ViewModelObserver
{
    public static void Observe<TViewModel>(ILifecycleOwner lifecycleOwner, TViewModel viewModel, Action<TViewModel, PropertyChangedEventArgs> observer)
        where TViewModel : INotifyPropertyChanged
    {
        if (lifecycleOwner.Lifecycle.CurrentState.Equals(Lifecycle.State.Destroyed))
            return;

        LifecycleObserver<TViewModel> lifecycleObserver = new(lifecycleOwner, viewModel, observer);
        lifecycleOwner.Lifecycle.AddObserver(lifecycleObserver);
    }

    sealed class LifecycleObserver<TViewModel> : Java.Lang.Object, ILifecycleEventObserver
        where TViewModel : INotifyPropertyChanged
    {

        readonly ILifecycleOwner _lifecycleOwner;
        readonly TViewModel _viewModel;
        readonly Action<TViewModel, PropertyChangedEventArgs> _observer;
        public LifecycleObserver(ILifecycleOwner lifecycleOwner, TViewModel viewModel, Action<TViewModel, PropertyChangedEventArgs> observer)
        {
            _lifecycleOwner = lifecycleOwner;
            _observer = observer;

            _viewModel = viewModel;
            viewModel.PropertyChanged += OnPropertyChanged;
        }

        readonly HashSet<PropertyChangedEventArgs> _unobservedPropertyChanges = [];
        void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not TViewModel viewModel)
                throw new InvalidOperationException($"Expected 'sender' to be of type '{typeof(TViewModel)}'");

            if (IsDestroyed)
                return;

            if (_lifecycleOwner.Lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Started!))
            {
                _observer(viewModel, e);
            }
            else
            {
                _unobservedPropertyChanges.Add(e);
            }
        }

        public void OnStateChanged(ILifecycleOwner source, Lifecycle.Event e)
        {
            var state = source.Lifecycle.CurrentState;
            if (state.Equals(Lifecycle.State.Destroyed!))
                OnDestroy(source);
            else if (state.IsAtLeast(Lifecycle.State.Started!))
                OnResume(source);
        }

        void OnResume(ILifecycleOwner owner)
        {
            if (IsDestroyed)
                return;

            foreach (var change in _unobservedPropertyChanges)
            {
                _observer(_viewModel, change);
            }
            _unobservedPropertyChanges.Clear();
        }

        public bool IsDestroyed { get; private set; }
        void OnDestroy(ILifecycleOwner owner)
        {
            if (IsDestroyed)
                return;

            _viewModel.PropertyChanged -= OnPropertyChanged;
            owner.Lifecycle.RemoveObserver(this);

            IsDestroyed = true;
        }
    }

}
