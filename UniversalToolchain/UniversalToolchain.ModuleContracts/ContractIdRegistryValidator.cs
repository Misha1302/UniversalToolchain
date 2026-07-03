namespace UniversalToolchain.ModuleContracts;

public static class ContractIdRegistryValidator
{
    public static IReadOnlyList<ToolchainDiagnostic> ValidateUniqueIds(IEnumerable<ContractId> ids)
    {
        ids = ids.ArgNotNull();

        return ids
            .GroupBy(static x => x.FullName, StringComparer.Ordinal)
            .Where(static x => x.Count() > 1)
            .Select(static x => new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.DuplicateId,
                ToolchainDiagnosticSeverity.Error,
                $"Duplicate contract id '{x.Key}'.",
                null,
                [new ToolchainDiagnosticHint("Contract ids must be unique inside their namespace.")]))
            .OrderBy(static x => x.Message, StringComparer.Ordinal)
            .ToArray();
    }
}
