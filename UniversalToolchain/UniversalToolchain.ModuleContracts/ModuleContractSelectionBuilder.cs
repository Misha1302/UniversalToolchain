namespace UniversalToolchain.ModuleContracts;

public sealed class ModuleContractSelectionBuilder
{
    public ModuleContractSelectionReport Build(
        IEnumerable<ModuleId> selectedModules,
        IEnumerable<IModuleContractDescriptorProvider> descriptorProviders,
        ModuleContractEnforcementPolicy enforcementPolicy)
    {
        selectedModules = selectedModules.ArgNotNull();
        descriptorProviders = descriptorProviders.ArgNotNull();
        enforcementPolicy = enforcementPolicy.ArgNotNull();

        var orderedSelectedModules = selectedModules
            .Distinct()
            .OrderBy(static x => x.Value, StringComparer.Ordinal)
            .ToArray();
        var selectedModuleSet = orderedSelectedModules
            .Select(static x => x.Value)
            .ToHashSet(StringComparer.Ordinal);

        var providerDescriptors = descriptorProviders
            .Select(provider => new
            {
                Provider = provider,
                Facets = provider.GetFacets()
                    .Where(x => selectedModuleSet.Contains(x.ModuleId.Value))
                    .ToArray()
            })
            .Where(static descriptor => descriptor.Facets.Length > 0)
            .ToArray();
        var facets = providerDescriptors
            .SelectMany(static descriptor => descriptor.Facets)
            .OrderBy(static x => x.ModuleId.Value, StringComparer.Ordinal)
            .ThenBy(static x => ContractFacetKindOrder.GetSortKey(x.Kind))
            .ThenBy(static x => x.GetType().FullName, StringComparer.Ordinal)
            .ToArray();

        var tableBuilder = new ModuleContractTableBuilder()
            .AddFacets(facets);
        foreach (var descriptor in providerDescriptors)
        {
            foreach (var moduleId in descriptor.Facets.Select(static facet => facet.ModuleId).Distinct())
                tableBuilder.AddNamespaceOwners(moduleId, descriptor.Provider.NamespaceOwners);
        }

        var table = tableBuilder.Build();

        var declaredModuleIds = facets
            .Select(static x => x.ModuleId.Value)
            .ToHashSet(StringComparer.Ordinal);
        var missingModules = orderedSelectedModules
            .Where(x => !declaredModuleIds.Contains(x.Value))
            .ToArray();

        var statuses = orderedSelectedModules
            .Select(moduleId =>
            {
                var hasExplicitStatus = enforcementPolicy.TryGetExplicitStatus(moduleId, out var explicitStatus);
                var inferredStatus = declaredModuleIds.Contains(moduleId.Value)
                    ? ModuleContractCompatibilityStatus.Declared
                    : ModuleContractCompatibilityStatus.Undeclared;

                return new SelectedModuleContractStatus(
                    moduleId,
                    hasExplicitStatus ? explicitStatus : inferredStatus);
            })
            .ToArray();

        var diagnostics = table.Diagnostics
            .Concat(missingModules
                .Where(moduleId => !enforcementPolicy.TryGetExplicitStatus(moduleId, out var status)
                                   || status < ModuleContractCompatibilityStatus.Declared)
                .Select(moduleId => CreateMissingDescriptorDiagnostic(moduleId, enforcementPolicy)))
            .Concat(statuses
                .Where(status => !declaredModuleIds.Contains(status.ModuleId.Value)
                                 && status.Status >= ModuleContractCompatibilityStatus.Declared)
                .Select(static status => new ToolchainDiagnostic(
                    ModuleContractDiagnosticCodes.DeclaredModuleMissingDescriptor,
                    ToolchainDiagnosticSeverity.Error,
                    $"Selected module '{status.ModuleId}' is marked '{status.Status}' but has no module contract descriptor.",
                    null,
                    [new ToolchainDiagnosticHint("Downgrade the explicit compatibility status or add an IModuleContractDescriptorProvider.")]))
            )
            .OrderBy(static x => x.Code, StringComparer.Ordinal)
            .ThenBy(static x => x.Message, StringComparer.Ordinal)
            .ToArray();

        return new ModuleContractSelectionReport(table, statuses, diagnostics);
    }

    private static ToolchainDiagnostic CreateMissingDescriptorDiagnostic(
        ModuleId moduleId,
        ModuleContractEnforcementPolicy enforcementPolicy)
    {
        if (enforcementPolicy.RequireNewModulesDeclared
            && !enforcementPolicy.ExplicitStatuses.ContainsKey(moduleId))
        {
            return new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.NewModuleMissingDescriptor,
                ToolchainDiagnosticSeverity.Error,
                $"Selected module '{moduleId}' has no module contract descriptor and is not explicitly accepted as Undeclared.",
                null,
                [new ToolchainDiagnosticHint("New modules must provide descriptors at Declared level or be explicitly accepted as Undeclared by the selected policy.")]);
        }

        return CreateUndeclaredDiagnostic(moduleId);
    }

    private static ToolchainDiagnostic CreateUndeclaredDiagnostic(ModuleId moduleId) =>
        new(
                ModuleContractDiagnosticCodes.UndeclaredModule,
                ToolchainDiagnosticSeverity.Warning,
                $"Selected module '{moduleId}' has no module contract descriptor and is explicitly treated as Undeclared.",
                null,
                [new ToolchainDiagnosticHint("Add an IModuleContractDescriptorProvider before moving this module to Declared or a stricter status.")]);
}
