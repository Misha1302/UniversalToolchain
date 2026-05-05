using System.Runtime.CompilerServices;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public abstract record DialectTypedStateKey(string Name)
{
    public abstract Type ValueType { get; }

    public override string ToString() => Name;
}

public sealed record DialectValueStateKey<TValue>(string Name) : DialectTypedStateKey(Name)
{
    public override Type ValueType => typeof(TValue);
}

public sealed record DialectListStateKey<TValue>(string Name) : DialectTypedStateKey(Name)
{
    public override Type ValueType => typeof(TValue);
}

public sealed record DialectSetStateKey<TValue>(string Name, IEqualityComparer<TValue>? Comparer = null) : DialectTypedStateKey(Name)
{
    public override Type ValueType => typeof(TValue);

    public IEqualityComparer<TValue> EffectiveComparer => Comparer ?? EqualityComparer<TValue>.Default;
}

internal static class DialectTypedStateGuards
{
    public static void EnsureKey(DialectTypedStateKey key, [CallerArgumentExpression(nameof(key))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(key.Name))
            Thrower.Argument(paramName.NotNull(), "Typed dialect state key name must not be empty.");
    }
}