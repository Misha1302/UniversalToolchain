namespace BasicTypesExtensions;

/// <summary>
/// Compatibility facade retained for callers that used EnumGenerator directly. Semantic identity is
/// now the stable ordinal name; no process-wide registry or insertion index participates in equality.
/// </summary>
[Obsolete("[UTL-DEP-009] Use ExtensibleEnum<TTag> directly or an instance-scoped ExtensibleEnumCatalog<TTag>. Removal requires stable-runtime-identity migration evidence.")]
public sealed class EnumGenerator
{
    private EnumGenerator()
    {
    }

    public static EnumGenerator Instance<T>() => new();

    public ExtensibleEnum<T> Get<T>(string name) => new(name);

    public ExtensibleEnum<T> CreateOrGet<T>(string name) => new(name);

    public string GetName(int value) => throw new NotSupportedException(
        "Insertion-index lookup was removed because it made semantic identity process-order dependent.");
}

public sealed class ExtensibleEnumCatalog<TTag>
{
    private readonly Dictionary<string, ExtensibleEnum<TTag>> _values = new(StringComparer.Ordinal);
    private bool _frozen;

    public ExtensibleEnum<TTag> Register(string name)
    {
        EnsureMutable();
        if (_values.ContainsKey(name))
            throw new InvalidOperationException($"Extensible enum identity '{name}' is already registered in this catalog.");
        var value = new ExtensibleEnum<TTag>(name);
        _values.Add(name, value);
        return value;
    }

    public ExtensibleEnum<TTag> GetOrAdd(string name)
    {
        EnsureMutable();
        if (_values.TryGetValue(name, out var existing))
            return existing;
        var value = new ExtensibleEnum<TTag>(name);
        _values.Add(name, value);
        return value;
    }

    public ExtensibleEnum<TTag> Get(string name) => _values.TryGetValue(name, out var value)
        ? value
        : throw new KeyNotFoundException($"Extensible enum identity '{name}' is not registered in this catalog.");

    public IReadOnlyList<ExtensibleEnum<TTag>> Freeze()
    {
        _frozen = true;
        return _values.OrderBy(static pair => pair.Key, StringComparer.Ordinal).Select(static pair => pair.Value).ToArray();
    }

    public bool IsFrozen => _frozen;

    private void EnsureMutable()
    {
        if (_frozen)
            throw new InvalidOperationException("The extensible enum catalog is frozen for runtime use.");
    }
}

public class SetAndList<T> where T : notnull
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

    public T this[int value]
    {
        get
        {
            lock (_lock)
                return _list[value];
        }
    }

    public int IndexOf(T name)
    {
        lock (_lock)
            return _valueToIndex.GetValueOrDefault(name, -1);
    }

    public void Add(T name) => _ = GetOrAdd(name);

    public int GetOrAdd(T name)
    {
        lock (_lock)
        {
            if (_valueToIndex.TryGetValue(name, out var existingIndex))
                return existingIndex;

            var newIndex = _list.Count;
            _list.Add(name);
            _valueToIndex.Add(name, newIndex);
            return newIndex;
        }
    }

    public bool Remove(T name)
    {
        lock (_lock)
        {
            if (!_valueToIndex.TryGetValue(name, out var index))
                return false;
            _list.RemoveAt(index);
            _valueToIndex.Remove(name);
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
