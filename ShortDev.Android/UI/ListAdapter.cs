using Android.Views;
using Android.Widget;
using System.Collections.Generic;

namespace ShortDev.Android.UI
{
    public sealed class ListAdapter<T> : BaseAdapter<T>
    {
        public AdapterDescriptor<T> Descriptor { get; }
        public IReadOnlyList<T> Data { get; }
        public ListAdapter(AdapterDescriptor<T> descriptor, IEnumerable<T> data)
        {
            Descriptor = descriptor;
            Data = new List<T>(data);
        }

        public override T this[int position]
            => Data[position];

        public override int Count
            => Data.Count;

        public override long GetItemId(int position)
            => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (convertView == null)
                convertView = LayoutInflater.From(parent.Context).Inflate(Descriptor.ViewId, null, false);
            Descriptor.InflateAction(convertView, this[position]);
            return convertView;
        }
    }
}
