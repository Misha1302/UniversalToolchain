using ExceptionsManager;

namespace BasicTypesExtensions;

public class EnumGenerator
{
    private static readonly Dictionary<Type, EnumGenerator> _dict = [];
    private readonly SetAndList<string> _setAndList = new();
    private int _num;

    private EnumGenerator()
    {
    }

    public static EnumGenerator Instance<T>()
    {
        Thrower.AssertAlways(!typeof(T).Name.Contains("ExtensibleEnum"));

        return _dict.TryGetValue(typeof(T), out var value)
            ? value
            : _dict[typeof(T)] = new EnumGenerator();
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
        if (_setAndList.IndexOf(name) != -1) return Get<T>(name);
        _setAndList.Add(name);
        return new ExtensibleEnum<T>(_num++);
    }

    public string GetName(int value) => _setAndList[value];
}

public class SetAndList<T> where T : notnull
{
    private readonly List<T> _list = [];
    private readonly Dictionary<T, int> _valueToIndex = [];

    public T this[int value] => _list[value];

    public int IndexOf(T name) => _valueToIndex.GetValueOrDefault(name, -1);

    public void Add(T name)
    {
        _list.Add(name);
        _valueToIndex[name] = _list.Count - 1;
    }
}