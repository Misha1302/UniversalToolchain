namespace VariablesRuntime.Runtime;

public sealed class VariablesContext
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

    public T LoadLocal<T>(string name)
    {
        name = name.ArgNotNull();

        if (!_values.TryGetValue(name, out var value))
            Thrower.InvalidOpEx($"Local variable '{name}' is not initialized.");

        if (value is T typedValue)
            return typedValue;

        return Thrower.InvalidCast<T>(
            $"Local variable '{name}' has runtime type '{value?.GetType().FullName ?? "null"}' and cannot be cast to '{typeof(T).FullName}'.");
    }

    public void StoreLocal<T>(string name, T value)
    {
        name = name.ArgNotNull();
        _values[name] = value;
    }
}