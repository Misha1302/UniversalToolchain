namespace UniversalToolchain.Ir.Abstractions;

/// <summary>
/// Identifies an analysis fact produced, preserved, required, or invalidated by an IR stage.
/// </summary>
public readonly record struct FactId : IComparable<FactId>
{
    public FactId(string id) : this()
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Fact identifier must not be empty.", nameof(id));

        Id = id.Trim();
    }

    public string Id { get; } = string.Empty;

    public int CompareTo(FactId other) => string.Compare(Id, other.Id, StringComparison.Ordinal);

    public override string ToString() => Id;
}
