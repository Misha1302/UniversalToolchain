namespace UniversalToolchain.ModuleContracts;

/// <summary>
/// Identifies a backend capability required by AIR or optimizer output.
/// </summary>
public readonly record struct BackendCapabilityId
{
    public BackendCapabilityId(string value)
    {
        Value = ContractIdentifierValidation.RequireNonEmpty(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}
