namespace UniversalToolchain.ModuleContracts;

public sealed class PipelineEffectFacet : IPipelineEffectContractFacet
{
    public PipelineEffectFacet(
        ModuleId moduleId,
        IReadOnlyList<PipelineEffectContract> effects)
    {
        ModuleId = moduleId;
        Effects = effects
            .ArgNotNull()
            .OrderBy(static x => x.EffectId.Value, StringComparer.Ordinal)
            .ToArray();
    }

    public ModuleId ModuleId { get; }

    public ContractFacetKind Kind => ContractFacetKind.PipelineEffects;

    public ContractSchemaVersion SchemaVersion { get; init; } = ModuleContractSchemaVersions.Current;

    public IReadOnlyList<PipelineEffectContract> Effects { get; }
}
