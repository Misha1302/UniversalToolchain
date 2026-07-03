namespace UniversalToolchain.ModuleContracts;

public sealed class ContractAliasCatalog
{
    private readonly IReadOnlyDictionary<string, CompatibilityAliasRecord> _aliases;

    public ContractAliasCatalog(IEnumerable<CompatibilityAliasRecord> aliases)
    {
        aliases = aliases.ArgNotNull();

        _aliases = aliases
            .OrderBy(static x => x.LegacyId, StringComparer.Ordinal)
            .ToDictionary(static x => x.LegacyId, static x => x, StringComparer.Ordinal);
    }

    public ContractAliasLookupResult Resolve(string legacyId)
    {
        if (string.IsNullOrWhiteSpace(legacyId))
            Thrower.Argument(nameof(legacyId), "Legacy contract id must not be empty.");

        if (!_aliases.TryGetValue(legacyId, out var alias))
            return new ContractAliasLookupResult(false, null, []);

        return new ContractAliasLookupResult(
            true,
            alias.Replacement,
            [
                new ToolchainDiagnostic(
                    ModuleContractDiagnosticCodes.DeprecatedAlias,
                    ToolchainDiagnosticSeverity.Warning,
                    $"Legacy contract id '{legacyId}' maps to '{alias.Replacement}'.",
                    null,
                    [new ToolchainDiagnosticHint("Use the typed contract id in new descriptor declarations.")])
            ]);
    }
}
