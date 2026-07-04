namespace UniversalToolchain.Air.Analysis;

public readonly record struct AirValueTypeId : IComparable<AirValueTypeId>
{
    public AirValueTypeId(string value) : this()
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("AIR value type identifier must not be empty.", nameof(value));

        Value = value.Trim();
    }

    public string Value { get; } = string.Empty;

    public int CompareTo(AirValueTypeId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}

public static class AirValueTypes
{
    public static AirValueTypeId Bool { get; } = new("core.bool");

    public static AirValueTypeId Int32 { get; } = new("core.i32");

    public static AirValueTypeId Float64 { get; } = new("core.f64");

    public static AirValueTypeId Object { get; } = new("core.object");
}
