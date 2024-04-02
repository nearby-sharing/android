using System.Collections;
using System.Collections.Concurrent;
using System.Numerics;

namespace ShortDev.Microsoft.ConnectedDevices.Internal;
internal sealed class AutoKeyRegistry<TKey, TValue> : IEnumerable<TValue> where TKey : IUnsignedNumber<TKey>
{
    readonly ConcurrentDictionary<TKey, TValue> _registry = new();

    TKey _nextKey = TKey.Zero;
    public AutoKeyRegistry()
        => Clear();

    public TValue Get(TKey key)
        => _registry[key];

    public void Add(TKey key, TValue value)
    {
        if (!_registry.TryAdd(key, value))
            throw new ArgumentException("Key already exists");
    }

    public TValue Create(Func<TKey, TValue> factory, out TKey key)
    {
        lock (_registry)
        {
            key = _nextKey++;
        }

        var value = factory(key);
        Add(key, value);
        return value;
    }

    public void Remove(TKey key)
    {
        _registry.Remove(key, out _);
    }

    public void Clear()
    {
        _registry.Clear();
        _nextKey = TKey.One; // 0xe
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public IEnumerator<TValue> GetEnumerator()
        => _registry.Values.GetEnumerator();
}
