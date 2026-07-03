namespace UniversalToolchain.ModuleContracts;

public sealed record PipelineEffectContract
{
    public PipelineEffectContract(
        CompilerEffectId effectId,
        CompilerPipelineStage stage,
        IReadOnlyList<CompilerFactId> requires,
        IReadOnlyList<CompilerFactId> produces,
        IReadOnlyList<CompilerFactId> preserves,
        IReadOnlyList<CompilerFactId> invalidates)
    {
        EffectId = effectId;
        Stage = stage;
        Requires = Normalize(requires);
        Produces = Normalize(produces);
        Preserves = Normalize(preserves);
        Invalidates = Normalize(invalidates);
    }

    public CompilerEffectId EffectId { get; }

    public CompilerPipelineStage Stage { get; }

    public IReadOnlyList<CompilerFactId> Requires { get; }

    public IReadOnlyList<CompilerFactId> Produces { get; }

    public IReadOnlyList<CompilerFactId> Preserves { get; }

    public IReadOnlyList<CompilerFactId> Invalidates { get; }

    private static IReadOnlyList<CompilerFactId> Normalize(IReadOnlyList<CompilerFactId> facts) =>
        facts
            .ArgNotNull()
            .OrderBy(static x => x.Value, StringComparer.Ordinal)
            .Distinct()
            .ToArray();
}
