using Android.Views;
using AndroidX.RecyclerView.Widget;
using System.Collections.Generic;

namespace ShortDev.Android.UI
{
    public sealed class RecyclerViewAdapter<T> : RecyclerView.Adapter
    {
        public AdapterDescriptor<T> Descriptor { get; }
        public IReadOnlyList<T> Data { get; }
        public RecyclerViewAdapter(AdapterDescriptor<T> descriptor, IEnumerable<T> data)
        {
            Descriptor = descriptor;
            Data = new List<T>(data);
        }

        public override int ItemCount
            => Data.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            Descriptor.InflateAction(holder.ItemView, Data[position]);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context).Inflate(Descriptor.ViewId, parent, false);
            return new ViewHolder(view);
        }

        class ViewHolder : RecyclerView.ViewHolder
        {
            public ViewHolder(View view) : base(view) { }
        }
    }
}
