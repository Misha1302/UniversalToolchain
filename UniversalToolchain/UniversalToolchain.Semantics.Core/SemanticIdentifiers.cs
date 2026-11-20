namespace UniversalToolchain.Semantics.Core;

internal static class SemanticValueValidation
{
    public static string Required(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be empty.", paramName);

        return value.Trim();
    }
}

/// <summary>
/// Stable identity of one semantic subject, for example a delegate value, a variable, or a call site.
/// </summary>
/// <remarks>
/// Scoping is not encoded in the value: the scope is carried by the query and by every evidence node.
/// </remarks>
public readonly record struct SemanticEntityId : IComparable<SemanticEntityId>
{
    public SemanticEntityId(string value) : this()
    {
        Value = SemanticValueValidation.Required(value, nameof(value));
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(SemanticEntityId other) => string.CompareOrdinal(Value, other.Value);

    public override string ToString() => Value;
}

/// <summary>
/// Identity of the component that published a fact or a rule. Used only for deterministic ordering
/// and provenance; it never grants selection authority.
/// </summary>
public readonly record struct SemanticProviderId : IComparable<SemanticProviderId>
{
    public SemanticProviderId(string value) : this()
    {
        Value = SemanticValueValidation.Required(value, nameof(value));
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(SemanticProviderId other) => string.CompareOrdinal(Value, other.Value);

    public override string ToString() => Value;
}

/// <summary>
/// Scope in which a judgment is asserted. Facts and rules never cross scopes in v1.
/// </summary>
public readonly record struct SemanticScopeId : IComparable<SemanticScopeId>
{
    public SemanticScopeId(string value) : this()
    {
        Value = SemanticValueValidation.Required(value, nameof(value));
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(SemanticScopeId other) => string.CompareOrdinal(Value, other.Value);

    public override string ToString() => Value;
}

/// <summary>
/// Monotonic identity of one graph compilation. Evidence established in an earlier epoch is stale and
/// cannot answer a query raised in a later epoch.
/// </summary>
public readonly record struct CompilationEpoch : IComparable<CompilationEpoch>
{
    public CompilationEpoch(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Compilation epoch must not be negative.");

        Value = value;
    }

    public int Value { get; }

    public CompilationEpoch Next() => new(checked(Value + 1));

    public int CompareTo(CompilationEpoch other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
