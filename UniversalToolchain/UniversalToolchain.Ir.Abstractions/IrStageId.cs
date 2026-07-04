namespace UniversalToolchain.Ir.Abstractions;

/// <summary>
/// Identifies a deterministic IR pipeline stage such as a converter, verifier, optimizer, or backend boundary.
/// </summary>
public readonly record struct IrStageId : IComparable<IrStageId>
{
    public IrStageId(string id) : this()
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("IR stage identifier must not be empty.", nameof(id));

        Id = id.Trim();
    }

    public string Id { get; } = string.Empty;

    public int CompareTo(IrStageId other) => string.Compare(Id, other.Id, StringComparison.Ordinal);

    public override string ToString() => Id;
}
