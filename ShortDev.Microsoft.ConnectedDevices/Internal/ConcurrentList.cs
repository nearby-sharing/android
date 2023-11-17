using System.Collections.Generic;

namespace ShortDev.Microsoft.ConnectedDevices.Internal;

internal sealed class ConcurrentList<T>
{
    readonly List<T> _data = [];

    public void Add(T item)
    {
        lock (_data)
            _data.Add(item);
    }

    public void Remove(T item)
    {
        lock (_data)
            _data.Remove(item);
    }

    public bool Contains(T item)
    {
        lock (_data)
            return _data.Contains(item);
    }
}
