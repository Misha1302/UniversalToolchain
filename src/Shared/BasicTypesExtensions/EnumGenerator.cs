namespace BasicTypesExtensions;

public class EnumGenerator
{
    private static readonly ConcurrentDictionary<Type, EnumGenerator> _dict = [];
    private readonly SetAndList<string> _setAndList = new();

    private EnumGenerator()
    {
    }

    public static EnumGenerator Instance<T>()
    {
        Thrower.AssertAlways(!typeof(T).Name.Contains("ExtensibleEnum"));

        return _dict.GetOrAdd(typeof(T), _ => new EnumGenerator());
    }

    public ExtensibleEnum<T> Get<T>(string name)
    {
        Thrower.AssertAlways(name != null);
        var index = _setAndList.IndexOf(name);
        Thrower.AssertAlways(index >= 0, $"'{name}' is not found in ExtEnum<{typeof(T).Name}>");
        return new ExtensibleEnum<T>(index);
    }

    public ExtensibleEnum<T> CreateOrGet<T>(string name)
    {
        var index = _setAndList.GetOrAdd(name);
        return new ExtensibleEnum<T>(index);
    }

    public string GetName(int value) => _setAndList[value];
}

public class SetAndList<T> where T : notnull
{
    private readonly List<T> _list = [];
    private readonly Lock _lock = new();
    private readonly Dictionary<T, int> _valueToIndex = [];

    public T this[int value]
    {
        get
        {
            lock (_lock)
            {
                return _list[value];
            }
        }
    }

    public int IndexOf(T name)
    {
        lock (_lock)
        {
            return _valueToIndex.GetValueOrDefault(name, -1);
        }
    }

    public void Add(T name)
    {
        lock (_lock)
        {
            _list.Add(name);
            _valueToIndex[name] = _list.Count - 1;
        }
    }

    public int GetOrAdd(T name)
    {
        lock (_lock)
        {
            if (_valueToIndex.TryGetValue(name, out var existingIndex))
                return existingIndex;

            var newIndex = _list.Count;
            _list.Add(name);
            _valueToIndex[name] = newIndex;
            return newIndex;
        }
    }
}