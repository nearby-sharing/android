using Android.Views;
using AndroidX.RecyclerView.Widget;
using ShortDev.Android.UI;

namespace NearShare.Utils;

public sealed class MultiViewAdapter<T> : RecyclerView.Adapter
{
    public required Func<int, int> LayoutSelector { get; init; }
    public required Func<View, int, ViewHolder<T>> ViewHolderFactory { get; init; }
    public required Func<T, int> ViewTypeResolver { get; init; }

    #region Data
    public required IReadOnlyList<T> ItemsSource
    {
        get => field ?? throw new NullReferenceException("No Data");
        set
        {
            bool firstChange = field is null;

            field = value;

            if (!firstChange)
                NotifyDataSetChanged();
        }
    }
    #endregion

    public override int ItemCount
        => ItemsSource.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var view = LayoutInflater.From(parent.Context)?
            .Inflate(LayoutSelector(viewType), parent, false) ?? throw new NullReferenceException("Inflated view was null");
        return ViewHolderFactory(view, viewType);
    }

    public override int GetItemViewType(int position)
        => ViewTypeResolver(ItemsSource[position]);

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is not ViewHolder<T> viewHolder)
            return;

        viewHolder.Bind(position, ItemsSource[position]);
    }

    public override void OnViewRecycled(Java.Lang.Object holder)
    {
        if (holder is not ViewHolder<T> viewHolder)
            return;

        viewHolder.Recycle();
    }
}
