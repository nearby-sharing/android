using System;
using System.Collections;
using System.Collections.Generic;

namespace ShortDev.Microsoft.ConnectedDevices.Internal;

internal sealed class AutoKeyRegistry<TValue> : IEnumerable<TValue>
{
    readonly Dictionary<ulong, TValue> _registry = new();

    ulong _nextKey;
    public AutoKeyRegistry()
        => Clear();

    public TValue Get(ulong key)
    {
        lock (this)
            return _registry[key];
    }

    public void Add(ulong key, TValue value)
    {
        lock (this)
        {
            if (_registry.ContainsKey(key))
                throw new ArgumentException("Key already exists");

            _registry.Add(key, value);
        }
    }

    public TValue Create(Func<ulong, TValue> factory, out ulong key)
    {
        lock (this)
        {
            key = _nextKey++;

            var value = factory(key);
            _registry.Add(key, value);
            return value;
        }
    }

    public void Remove(ulong key)
    {
        lock (this)
        {
            _registry.Remove(key);
        }
    }

    public void Clear()
    {
        lock (this)
        {
            _registry.Clear();
            _nextKey = 0xe;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public IEnumerator<TValue> GetEnumerator()
        => _registry.Values.GetEnumerator();
}
