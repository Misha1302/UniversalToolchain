namespace UniversalToolchain.ModuleContracts;

internal static class ContractNamespaceTableValidator
{
    public static IReadOnlyList<ToolchainDiagnostic> Validate(
        SelectedModuleContractTable table,
        IReadOnlyDictionary<ModuleId, IReadOnlyList<ContractNamespaceOwner>> ownersByModule,
        IReadOnlyList<ContractNamespaceOwner> reservations)
    {
        table = table.ArgNotNull();
        ownersByModule = ownersByModule.ArgNotNull();
        reservations = reservations.ArgNotNull();

        var diagnostics = new List<ToolchainDiagnostic>();
        foreach (var moduleGroup in table.Facets.GroupBy(static facet => facet.ModuleId))
        {
            if (!ownersByModule.TryGetValue(moduleGroup.Key, out var owners) || owners.Count == 0)
                continue;

            ValidateIdentifier(moduleGroup.Key.Value, moduleGroup.Key, owners, reservations, diagnostics);
            foreach (var identifier in moduleGroup.SelectMany(static facet => facet.OwnedIdentifiers()).Distinct(StringComparer.Ordinal))
                ValidateIdentifier(identifier, moduleGroup.Key, owners, reservations, diagnostics);
        }

        return diagnostics
            .DistinctBy(static diagnostic => (diagnostic.Code, diagnostic.Message))
            .OrderBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidateIdentifier(
        string identifier,
        ModuleId moduleId,
        IReadOnlyList<ContractNamespaceOwner> owners,
        IReadOnlyList<ContractNamespaceOwner> reservations,
        List<ToolchainDiagnostic> diagnostics)
    {
        if (owners.Any(owner =>
                ContractNamespacePolicy.ValidateOwnership(identifier, owner, reservations).Count == 0))
        {
            return;
        }

        var expected = string.Join(
            ", ",
            owners.Select(static owner => owner.NamespacePrefix ?? owner.Name)
                .OrderBy(static value => value, StringComparer.Ordinal));
        diagnostics.Add(new ToolchainDiagnostic(
            ModuleContractDiagnosticCodes.InvalidNamespaceOwnership,
            ToolchainDiagnosticSeverity.Error,
            $"Module '{moduleId}' declares contract identifier '{identifier}' outside its owned namespace set: {expected}.",
            null,
            [new ToolchainDiagnosticHint("Declare the package namespace through IModuleContractDescriptorProvider.NamespaceOwners or move the identifier to an owned namespace.")]));
    }
}
