namespace UniversalToolchain.ModuleContracts;

/// <summary>
/// Identifies semantic bytecode metadata emitted or consumed by modules.
/// </summary>
public readonly record struct BytecodeTagId
{
    public BytecodeTagId(string value)
    {
        Value = ContractIdentifierValidation.RequireNonEmpty(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}
