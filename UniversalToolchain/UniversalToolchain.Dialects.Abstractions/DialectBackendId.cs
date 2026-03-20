namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Open-ended backend identifier used by dialect policies and runtime descriptors.
/// </summary>
public readonly record struct DialectBackendId : IComparable<DialectBackendId>
{
    public DialectBackendId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            Thrower.Argument(nameof(value), "Backend identifier must not be empty.");

        Value = value.Trim();
    }

    public string Value { get; }

    public int CompareTo(DialectBackendId other) => StringComparer.Ordinal.Compare(Value, other.Value);

    public override string ToString() => Value;
}
