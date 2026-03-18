using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public abstract record DialectTypedStateKey(string Name)
{
    public override string ToString() => Name;
}

public sealed record DialectValueStateKey<TValue>(string Name) : DialectTypedStateKey(Name);

public sealed record DialectListStateKey<TValue>(string Name) : DialectTypedStateKey(Name);

public sealed record DialectSetStateKey<TValue>(string Name, IEqualityComparer<TValue>? Comparer = null) : DialectTypedStateKey(Name)
{
    public IEqualityComparer<TValue> EffectiveComparer => Comparer ?? EqualityComparer<TValue>.Default;
}

internal static class DialectTypedStateGuards
{
    public static void EnsureKey(DialectTypedStateKey? key, string paramName)
    {
        if (key == null)
        {
            Thrower.ArgumentNull(paramName);
        }

        if (string.IsNullOrWhiteSpace(key.Name))
        {
            Thrower.Argument(paramName, "Typed dialect state key name must not be empty.");
        }
    }
}
