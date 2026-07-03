namespace UniversalToolchain.ModuleContracts;

/// <summary>
/// Identifies a contract verifier rule contribution.
/// </summary>
public readonly record struct VerifierRuleId
{
    public VerifierRuleId(string value)
    {
        Value = ContractIdentifierValidation.RequireNonEmpty(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}
