using Android.Content;
using Android.Util;
using Android.Views;

namespace NearShare;

internal sealed class MyLayoutInflacter(LayoutInflater original, Context? newContext) : LayoutInflater(original, newContext)
{
    public override LayoutInflater? CloneInContext(Context? newContext)
        => new MyLayoutInflacter(this, newContext);

    public override View? OnCreateView(Context viewContext, View? parent, string name, IAttributeSet? attrs)
    {
        return base.OnCreateView(viewContext, parent, name, attrs);
    }

    protected override View? OnCreateView(string? name, IAttributeSet? attrs)
    {
        return base.OnCreateView(name, attrs);
    }

    protected override View? OnCreateView(View? parent, string? name, IAttributeSet? attrs)
    {
        return base.OnCreateView(parent, name, attrs);
    }
}
