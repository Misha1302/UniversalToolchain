namespace SettableGettableModule;

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