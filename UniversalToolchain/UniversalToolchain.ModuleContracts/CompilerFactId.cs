namespace UniversalToolchain.ModuleContracts;

public readonly record struct CompilerFactId
{
    public CompilerFactId(string value)
    {
        Value = ContractIdentifierValidation.RequireDottedIdentifier(value, nameof(value));
    }

    public CompilerFactId(string @namespace, string name)
        : this($"{@namespace}.{name}")
    {
    }

    public string Value { get; }

    public override string ToString() => Value;
}
