namespace BasicTypesExtensions;

/// <summary>
/// Maintains insertion order and set uniqueness under one synchronization boundary.
/// </summary>
public sealed class SetAndList<T> where T : notnull
{
    private readonly List<T> _list = [];
    private readonly Lock _lock = new();
    private readonly Dictionary<T, int> _valueToIndex = [];

    public int Count
    {
        get
        {
            lock (_lock)
                return _list.Count;
        }
    }

    public T this[int index]
    {
        get
        {
            lock (_lock)
                return _list[index];
        }
    }

    public int IndexOf(T value)
    {
        lock (_lock)
            return _valueToIndex.GetValueOrDefault(value, -1);
    }

    public void Add(T value) => _ = GetOrAdd(value);

    public int GetOrAdd(T value)
    {
        lock (_lock)
        {
            if (_valueToIndex.TryGetValue(value, out var existingIndex))
                return existingIndex;

            var newIndex = _list.Count;
            _list.Add(value);
            _valueToIndex.Add(value, newIndex);
            return newIndex;
        }
    }

    public bool Remove(T value)
    {
        lock (_lock)
        {
            if (!_valueToIndex.TryGetValue(value, out var index))
                return false;
            _list.RemoveAt(index);
            _valueToIndex.Remove(value);
            for (var current = index; current < _list.Count; current++)
                _valueToIndex[_list[current]] = current;
            return true;
        }
    }

    public IReadOnlyList<T> Snapshot()
    {
        lock (_lock)
            return _list.ToArray();
    }
}
