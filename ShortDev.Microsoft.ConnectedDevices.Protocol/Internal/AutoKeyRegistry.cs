using System;
using System.Collections;
using System.Collections.Generic;

namespace ShortDev.Microsoft.ConnectedDevices.Protocol.Internal;

internal sealed class AutoKeyRegistry<TValue> : IEnumerable<TValue>
{
    readonly Dictionary<ulong, TValue> _registry = new();
    readonly Stack<ulong> _freeKeys = new();

    ulong _nextFreeKey;
    public AutoKeyRegistry()
        => Clear();

    public TValue Get(ulong key)
    {
        lock (this)
            return _registry[key];
    }

    public TValue Create(Func<ulong, TValue> factory, out ulong key)
    {
        lock (this)
        {
            if (_freeKeys.Count > 0)
                key = _freeKeys.Pop();
            else
                key = _nextFreeKey++;

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
            _freeKeys.Push(key);
        }
    }

    public void Clear()
    {
        lock (this)
        {
            _registry.Clear();
            _freeKeys.Clear();
            _nextFreeKey = 0xe;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public IEnumerator<TValue> GetEnumerator()
        => _registry.Values.GetEnumerator();
}
