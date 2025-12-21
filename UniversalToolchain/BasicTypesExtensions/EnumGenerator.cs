using ExceptionsManager;

namespace BasicTypesExtensions;

public class EnumGenerator
{
    private static readonly Dictionary<Type, EnumGenerator> _dict = [];

    private readonly List<string?> _names = [];
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
        var index = _names.IndexOf(name);
        Thrower.AssertAlways(index >= 0, $"'{name}' is not found in ExtEnum<{typeof(T).Name}>");
        return new ExtensibleEnum<T>(index);
    }

    public ExtensibleEnum<T> CreateOrGet<T>(string name)
    {
        if (_names.IndexOf(name) != -1) return Get<T>(name);
        _names.Add(name);
        return new ExtensibleEnum<T>(_num++);
    }

    public string GetName(int value)
    {
        return _names[value] ?? value.ToString();
    }
}