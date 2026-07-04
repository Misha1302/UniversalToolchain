namespace UniversalToolchain.Ir.Abstractions;

/// <summary>
/// Identifies an intermediate representation kind without coupling the toolchain core to its implementation type.
/// </summary>
public readonly record struct IrKind : IComparable<IrKind>
{
    public IrKind(string id) : this()
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("IR kind identifier must not be empty.", nameof(id));

        Id = id.Trim();
    }

    public string Id { get; } = string.Empty;

    public int CompareTo(IrKind other) => string.Compare(Id, other.Id, StringComparison.Ordinal);

    public override string ToString() => Id;
}
