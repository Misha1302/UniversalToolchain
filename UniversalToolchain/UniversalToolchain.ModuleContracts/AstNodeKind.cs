namespace UniversalToolchain.ModuleContracts;

/// <summary>
/// Identifies an AST node kind that crosses a module or pipeline boundary.
/// </summary>
public readonly record struct AstNodeKind
{
    public AstNodeKind(string value)
    {
        Value = ContractIdentifierValidation.RequireNonEmpty(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}
