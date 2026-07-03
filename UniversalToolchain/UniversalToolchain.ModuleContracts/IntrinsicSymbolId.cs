namespace UniversalToolchain.ModuleContracts;

/// <summary>
/// Identifies an intrinsic symbol used in AIR contract declarations.
/// </summary>
public readonly record struct IntrinsicSymbolId
{
    public IntrinsicSymbolId(string value)
    {
        Value = ContractIdentifierValidation.RequireNonEmpty(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}
