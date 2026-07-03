namespace UniversalToolchain.ModuleContracts;

/// <summary>
/// Identifies a declared bytecode operation shape or emission pattern.
/// </summary>
public readonly record struct BytecodePatternId
{
    public BytecodePatternId(string value)
    {
        Value = ContractIdentifierValidation.RequireNonEmpty(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}
