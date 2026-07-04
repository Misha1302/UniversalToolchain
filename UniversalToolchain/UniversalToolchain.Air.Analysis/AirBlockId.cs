namespace UniversalToolchain.Air.Analysis;

public readonly record struct AirBlockId : IComparable<AirBlockId>
{
    public AirBlockId(string value) : this()
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("AIR block identifier must not be empty.", nameof(value));

        Value = value.Trim();
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(AirBlockId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}
