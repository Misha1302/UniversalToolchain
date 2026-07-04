namespace UniversalToolchain.Semantics.Abstractions;

internal static class SemanticIdValidation
{
    public static string Normalize(string value, string parameterName, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{displayName} must not be empty.", parameterName);

        return value.Trim();
    }
}

public readonly record struct SemanticTypeId : IComparable<SemanticTypeId>
{
    public SemanticTypeId(string value) : this()
    {
        Value = SemanticIdValidation.Normalize(value, nameof(value), "Semantic type identifier");
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(SemanticTypeId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}

public readonly record struct CallableId : IComparable<CallableId>
{
    public CallableId(string value) : this()
    {
        Value = SemanticIdValidation.Normalize(value, nameof(value), "Callable identifier");
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(CallableId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}

public readonly record struct SemanticAttributeKey : IComparable<SemanticAttributeKey>
{
    public SemanticAttributeKey(string value) : this()
    {
        Value = SemanticIdValidation.Normalize(value, nameof(value), "Semantic attribute key");
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(SemanticAttributeKey other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}
