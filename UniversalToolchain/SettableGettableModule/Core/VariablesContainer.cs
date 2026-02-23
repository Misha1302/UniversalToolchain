using System.Runtime.InteropServices;

namespace SettableGettableModule.Core;

public static class VariablesContainer<T>
{
    private static readonly Dictionary<string, T> _variables = [];

    public static void Set(string key, T value)
    {
        CollectionsMarshal.GetValueRefOrAddDefault(_variables, key, out _) = value;
    }

    public static T Get(string key)
    {
        var value = CollectionsMarshal.GetValueRefOrNullRef(_variables, key);
        return value;
    }

    public static VariableReference<T> GetRef(string key)
    {
        return new VariableReference<T>(value => CollectionsMarshal.GetValueRefOrAddDefault(_variables, key, out _) = value);
    }
}