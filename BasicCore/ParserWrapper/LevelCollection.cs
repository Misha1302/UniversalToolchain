// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Collections;

namespace BasicCore.ParserWrapper;

public class LevelCollection<TKey, TValue> : IEnumerable<KeyValuePair<TKey, List<TValue>>> where TKey : notnull
{
    private readonly SortedDictionary<TKey, List<TValue>> _map = new();

    public List<TValue> this[TKey key]
    {
        get
        {
            _map.TryAdd(key, []);
            return _map[key];
        }
    }

    public IEnumerator<KeyValuePair<TKey, List<TValue>>> GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<TKey, List<TValue>>>)_map).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(TKey key, TValue value)
    {
        this[key].Add(value);
    }

    public void Clear()
    {
        _map.Clear();
    }
}