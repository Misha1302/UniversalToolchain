namespace SettableGettableModule.Core;

public static class VariablesContainer<T>
{
    private static readonly Dictionary<string, T> _variables = [];

    /// <summary>
    /// Stores value by key, replacing an existing value when key is already present.
    /// </summary>
    public static void Set(string key, T value)
    {
        CollectionsMarshal.GetValueRefOrAddDefault(_variables, key, out _) = value;
    }

    /// <summary>
    /// Gets value by key or throws <see cref="KeyNotFoundException"/> when key is missing.
    /// </summary>
    public static T Get(string key)
    {
        if (TryGet(key, out var value))
            return value;

        throw new KeyNotFoundException($"Variable with key '{key}' was not found.");
    }

    /// <summary>
    /// Tries to get value by key without throwing when key is missing.
    /// </summary>
    public static bool TryGet(string key, out T value)
    {
        return _variables.TryGetValue(key, out value!);
    }

    public static VariableReference<T> GetRef(string key)
    {
        return new VariableReference<T>(value => CollectionsMarshal.GetValueRefOrAddDefault(_variables, key, out _) = value);
    }
}
