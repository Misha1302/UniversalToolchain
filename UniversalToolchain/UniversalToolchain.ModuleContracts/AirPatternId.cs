namespace UniversalToolchain.ModuleContracts;

/// <summary>
/// Identifies an AIR operation shape, intrinsic family or semantic emission pattern.
/// </summary>
public readonly record struct AirPatternId
{
    public AirPatternId(string value)
    {
        Value = ContractIdentifierValidation.RequireNonEmpty(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}
