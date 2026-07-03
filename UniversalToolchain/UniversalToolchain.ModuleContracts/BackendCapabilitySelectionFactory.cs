namespace UniversalToolchain.ModuleContracts;

public sealed class BackendCapabilitySelectionFactory(AirBackendPolicy policy) : IBackendCapabilitySelectionFactory
{
    private readonly AirBackendPolicy _policy = policy.ArgNotNull();

    public BackendCapabilitySelection Create(
        SelectedModuleContractTable table,
        IReadOnlyList<string> compilerSupportedIntrinsics)
    {
        table = table.ArgNotNull();
        compilerSupportedIntrinsics = compilerSupportedIntrinsics.ArgNotNull();

        var selectedBackendFacets = table.BackendCapabilityFacets
            .Where(static facet => facet.ModuleId != KnownCoreModuleIds.BackendCapabilities)
            .ToArray();
        if (selectedBackendFacets.Length > 1)
        {
            throw new InvalidOperationException(
                $"Backend capability selection requires one selected backend facet, but found {selectedBackendFacets.Length}: {string.Join(", ", selectedBackendFacets.Select(static facet => facet.ModuleId.Value))}.");
        }

        var compilerIntrinsicSurface = compilerSupportedIntrinsics
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => new IntrinsicSymbolId(x))
            .Distinct()
            .ToArray();
        var capabilityFacets = table.BackendCapabilityFacets
            .Where(facet => facet.ModuleId == KnownCoreModuleIds.BackendCapabilities || selectedBackendFacets.Contains(facet))
            .ToArray();
        var contractIntrinsicSurface = capabilityFacets
            .SelectMany(static facet => facet.Capabilities)
            .SelectMany(static capability => capability.SupportedIntrinsics)
            .Distinct()
            .ToArray();
        var supportedIntrinsics = contractIntrinsicSurface.Length == 0
            ? compilerIntrinsicSurface
            : compilerIntrinsicSurface.Intersect(contractIntrinsicSurface).ToArray();
        var supportedSet = supportedIntrinsics.ToHashSet();
        var inferredCapabilities = table.AirFacets
            .SelectMany(static facet => facet.AirEmissions)
            .Where(emission => emission.MayEmitIntrinsics.All(supportedSet.Contains))
            .SelectMany(static emission => emission.RequiredCapabilities)
            .Distinct()
            .ToArray();

        return new BackendCapabilitySelection(
            inferredCapabilities,
            supportedIntrinsics,
            _policy);
    }
}
