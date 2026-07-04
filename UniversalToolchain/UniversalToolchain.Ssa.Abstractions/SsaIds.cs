namespace UniversalToolchain.Ssa.Abstractions;

internal static class SsaIdValidation
{
    public static string Normalize(string value, string parameterName, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{displayName} must not be empty.", parameterName);

        return value.Trim();
    }
}

public readonly record struct SsaModuleId : IComparable<SsaModuleId>
{
    public SsaModuleId(string value) : this()
    {
        Value = SsaIdValidation.Normalize(value, nameof(value), "SSA module identifier");
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(SsaModuleId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}

public readonly record struct SsaFunctionId : IComparable<SsaFunctionId>
{
    public SsaFunctionId(string value) : this()
    {
        Value = SsaIdValidation.Normalize(value, nameof(value), "SSA function identifier");
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(SsaFunctionId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}

public readonly record struct SsaBlockId : IComparable<SsaBlockId>
{
    public SsaBlockId(string value) : this()
    {
        Value = SsaIdValidation.Normalize(value, nameof(value), "SSA block identifier");
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(SsaBlockId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}

public readonly record struct SsaValueId : IComparable<SsaValueId>
{
    public SsaValueId(string value) : this()
    {
        Value = SsaIdValidation.Normalize(value, nameof(value), "SSA value identifier");
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(SsaValueId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}

public readonly record struct SsaOperationId : IComparable<SsaOperationId>
{
    public SsaOperationId(string value) : this()
    {
        Value = SsaIdValidation.Normalize(value, nameof(value), "SSA operation identifier");
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(SsaOperationId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}

public readonly record struct SsaOpId : IComparable<SsaOpId>
{
    public SsaOpId(string value) : this()
    {
        Value = SsaIdValidation.Normalize(value, nameof(value), "SSA operation descriptor identifier");
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(SsaOpId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}

public readonly record struct SsaTypeId : IComparable<SsaTypeId>
{
    public SsaTypeId(string value) : this()
    {
        Value = SsaIdValidation.Normalize(value, nameof(value), "SSA type identifier");
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(SsaTypeId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}

public readonly record struct SsaAttributeKey : IComparable<SsaAttributeKey>
{
    public SsaAttributeKey(string value) : this()
    {
        Value = SsaIdValidation.Normalize(value, nameof(value), "SSA attribute key");
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(SsaAttributeKey other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}
