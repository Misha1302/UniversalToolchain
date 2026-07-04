namespace UniversalToolchain.Ir.Abstractions;

/// <summary>
/// Identifies a capability required or provided by an IR pass, converter, verifier, or backend.
/// </summary>
public readonly record struct CapabilityId : IComparable<CapabilityId>
{
    public CapabilityId(string id) : this()
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Capability identifier must not be empty.", nameof(id));

        Id = id.Trim();
    }

    public string Id { get; } = string.Empty;

    public int CompareTo(CapabilityId other) => string.Compare(Id, other.Id, StringComparison.Ordinal);

    public override string ToString() => Id;
}
