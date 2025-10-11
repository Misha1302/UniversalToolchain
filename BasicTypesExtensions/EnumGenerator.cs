// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

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
        Thrower.AssertAlways(index >= 0);
        return new ExtensibleEnum<T>(index);
    }

    public ExtensibleEnum<T> CreateNewUnique<T>(string? name)
    {
        _names.Add(name);
        return new ExtensibleEnum<T>(_num++);
    }

    public ExtensibleEnum<T> CreateNewUniqueUnnamed<T>()
    {
        return CreateNewUnique<T>(null);
    }

    public string ToString(int value)
    {
        return _names[value] ?? value.ToString();
    }
}