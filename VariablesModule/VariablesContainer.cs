// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace VariablesModule;

public static class VariablesContainer<T>
{
    private static readonly Dictionary<string, T> _variables = [];

    public static void Set(string key, T value)
    {
        _variables[key] = value;
    }

    public static T Get(string key)
    {
        return _variables[key];
    }

    public static VariableReference<T> GetRef(string key)
    {
        return new VariableReference<T>(value => _variables[key] = value);
    }
}