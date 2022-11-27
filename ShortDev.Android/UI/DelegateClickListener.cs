using Android.Views;

namespace ShortDev.Android.UI;

public sealed class DelegateClickListener : Java.Lang.Object, View.IOnClickListener
{
    EventHandler _handler;
    public DelegateClickListener(EventHandler handler)
        => _handler = handler;

    public void OnClick(View? v)
        => _handler?.Invoke(v, EventArgs.Empty);
}
