namespace UniversalToolchain.ModuleContracts;

public sealed class CompilerFactOwnershipFacet : ICompilerFactOwnershipFacet
{
    public CompilerFactOwnershipFacet(
        ModuleId moduleId,
        IReadOnlyList<CompilerFactOwnershipContract> facts)
    {
        ModuleId = moduleId;
        Facts = facts
            .ArgNotNull()
            .OrderBy(static x => x.FactId.Value, StringComparer.Ordinal)
            .ToArray();
    }

    public ModuleId ModuleId { get; }

    public ContractFacetKind Kind => ContractFacetKind.CompilerFacts;

    public ContractSchemaVersion SchemaVersion { get; init; } = ModuleContractSchemaVersions.Current;

    public IReadOnlyList<CompilerFactOwnershipContract> Facts { get; }
}
