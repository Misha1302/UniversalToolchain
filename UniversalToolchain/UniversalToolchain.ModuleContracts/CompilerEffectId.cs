namespace UniversalToolchain.ModuleContracts;

public readonly record struct CompilerEffectId
{
    public CompilerEffectId(string value)
    {
        Value = ContractIdentifierValidation.RequireDottedIdentifier(value, nameof(value));
    }

    public CompilerEffectId(string @namespace, string name)
        : this($"{@namespace}.{name}")
    {
    }

    public string Value { get; }

    public override string ToString() => Value;
}
