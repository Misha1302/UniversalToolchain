namespace BasicTypesExtensions;

/// <summary>
/// Stores extensible-enum identities inside an explicit owner scope and freezes them deterministically.
/// </summary>
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
