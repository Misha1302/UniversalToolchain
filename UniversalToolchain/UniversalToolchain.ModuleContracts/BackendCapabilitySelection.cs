namespace UniversalToolchain.ModuleContracts;

public sealed record BackendCapabilitySelection
{
    public BackendCapabilitySelection(
        IEnumerable<BackendCapabilityId> capabilityIds,
        IEnumerable<IntrinsicSymbolId> supportedIntrinsics,
        AirBackendPolicy? policy = null)
    {
        capabilityIds = capabilityIds.ArgNotNull();
        supportedIntrinsics = supportedIntrinsics.ArgNotNull();

        CapabilityIds = capabilityIds
            .OrderBy(static x => x.Value, StringComparer.Ordinal)
            .Distinct()
            .ToArray();
        SupportedIntrinsics = supportedIntrinsics
            .OrderBy(static x => x.Value, StringComparer.Ordinal)
            .Distinct()
            .ToArray();
        Policy = policy ?? AirBackendPolicy.CapabilityGated;
    }

    public IReadOnlyList<BackendCapabilityId> CapabilityIds { get; }

    public IReadOnlyList<IntrinsicSymbolId> SupportedIntrinsics { get; }

    public AirBackendPolicy Policy { get; }

    public static BackendCapabilitySelection FromContracts(
        SelectedModuleContractTable contractTable,
        IEnumerable<BackendCapabilityId> selectedCapabilityIds,
        AirBackendPolicy? policy = null)
    {
        contractTable = contractTable.ArgNotNull();
        selectedCapabilityIds = selectedCapabilityIds.ArgNotNull();

        var selectedIds = selectedCapabilityIds
            .OrderBy(static x => x.Value, StringComparer.Ordinal)
            .Distinct()
            .ToArray();
        var selectedIdSet = selectedIds.ToHashSet();
        var supportedIntrinsics = contractTable.BackendCapabilityFacets
            .SelectMany(static x => x.Capabilities)
            .Where(x => selectedIdSet.Contains(x.CapabilityId))
            .SelectMany(static x => x.SupportedIntrinsics)
            .ToArray();

        return new BackendCapabilitySelection(selectedIds, supportedIntrinsics, policy);
    }
}
