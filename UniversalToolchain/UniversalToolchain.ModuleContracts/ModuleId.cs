namespace UniversalToolchain.ModuleContracts;

/// <summary>
/// Identifies a selected language, optimizer, backend or support module in contract tables.
/// </summary>
public readonly record struct ModuleId
{
    public ModuleId(string value)
    {
        Value = ContractIdentifierValidation.RequireNonEmpty(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}
