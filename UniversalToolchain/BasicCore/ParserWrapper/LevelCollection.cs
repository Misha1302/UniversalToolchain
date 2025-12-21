using System.Collections;

namespace BasicCore.ParserWrapper;

public interface IReadOnlyLevelCollection<TKey, TValue> : IEnumerable<KeyValuePair<TKey, List<TValue>>> where TKey : notnull
{
    public List<TValue> this[TKey key] { get; }
}

public class LevelCollection<TKey, TValue> : IReadOnlyLevelCollection<TKey, TValue> where TKey : notnull
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