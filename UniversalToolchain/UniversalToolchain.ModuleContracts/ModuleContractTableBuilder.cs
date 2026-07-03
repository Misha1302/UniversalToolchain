namespace UniversalToolchain.ModuleContracts;

public sealed class ModuleContractTableBuilder
{
    private readonly List<IModuleContractFacet> _facets = [];

    public ContractSchemaVersion SupportedSchemaVersion { get; init; } = ModuleContractSchemaVersions.Current;

    public ModuleContractTableBuilder AddFacet(IModuleContractFacet facet)
    {
        facet = facet.ArgNotNull();

        _facets.Add(facet);
        return this;
    }

    public ModuleContractTableBuilder AddFacets(IEnumerable<IModuleContractFacet> facets)
    {
        facets = facets.ArgNotNull();

        foreach (var facet in facets)
            AddFacet(facet);

        return this;
    }

    public SelectedModuleContractTable Build()
    {
        var orderedFacets = _facets
            .OrderBy(static x => x.ModuleId.Value, StringComparer.Ordinal)
            .ThenBy(static x => ContractFacetKindOrder.GetSortKey(x.Kind))
            .ThenBy(static x => x.GetType().FullName, StringComparer.Ordinal)
            .ToArray();

        var diagnostics = orderedFacets
            .GroupBy(static x => (x.ModuleId, x.Kind))
            .Where(static x => x.Count() > 1)
            .Select(static x => new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.DuplicateFacet,
                ToolchainDiagnosticSeverity.Error,
                $"Module '{x.Key.ModuleId}' declares duplicate '{x.Key.Kind}' contract facets.",
                null,
                [new ToolchainDiagnosticHint("Declare one facet per module and facet kind, or split independent data into a single normalized facet.")]))
            .Concat(orderedFacets
                .Where(x => x.SchemaVersion > SupportedSchemaVersion)
                .Select(x => new ToolchainDiagnostic(
                    ModuleContractDiagnosticCodes.SchemaDowngrade,
                    ToolchainDiagnosticSeverity.Error,
                    $"Module '{x.ModuleId}' declares schema version '{x.SchemaVersion}', but the selected contract table supports '{SupportedSchemaVersion}'.",
                    null,
                    [new ToolchainDiagnosticHint("Use a compatible contract validator or add an explicit compatibility alias before reading newer schema data.")]))
            )
            .Concat(ContractFacetKindOrder.ValidateComplete())
            .OrderBy(static x => x.Code, StringComparer.Ordinal)
            .ThenBy(static x => x.Message, StringComparer.Ordinal)
            .ToArray();

        var table = new SelectedModuleContractTable(SupportedSchemaVersion, orderedFacets, diagnostics);
        var pipelineDiagnostics = PipelineEffectContractValidator.Validate(table);
        return new SelectedModuleContractTable(
            SupportedSchemaVersion,
            orderedFacets,
            diagnostics.Concat(pipelineDiagnostics)
                .OrderBy(static x => x.Code, StringComparer.Ordinal)
                .ThenBy(static x => x.Message, StringComparer.Ordinal)
                .ToArray());
    }
}
