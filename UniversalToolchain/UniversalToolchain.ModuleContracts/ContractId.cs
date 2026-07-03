namespace UniversalToolchain.ModuleContracts;

public sealed record ContractId
{
    public ContractId(
        string @namespace,
        string name,
        ContractSchemaVersion introducedIn,
        ContractSchemaVersion? deprecatedIn = null,
        string? replacementId = null)
    {
        Namespace = ContractIdentifierValidation.RequireNonEmpty(@namespace, nameof(@namespace));
        Name = ContractIdentifierValidation.RequireNonEmpty(name, nameof(name));
        IntroducedIn = introducedIn;
        DeprecatedIn = deprecatedIn;
        ReplacementId = string.IsNullOrWhiteSpace(replacementId) ? null : replacementId;
    }

    public string Namespace { get; }

    public string Name { get; }

    public ContractSchemaVersion IntroducedIn { get; }

    public ContractSchemaVersion? DeprecatedIn { get; }

    public string? ReplacementId { get; }

    public string FullName => $"{Namespace}.{Name}";

    public override string ToString() => FullName;
}
