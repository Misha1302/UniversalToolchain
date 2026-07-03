namespace UniversalToolchain.ModuleContracts;

public sealed class CoreCompilerStageFactSeedProvider : ICompilerStageFactSeedProvider
{
    public CompilerFactState CreateInitialState(CompilerPipelineStage stage) =>
        stage switch
        {
            CompilerPipelineStage.Bytecode => Create(
                KnownCoreCompilerFacts.SourceAvailable,
                KnownCoreCompilerFacts.LexemesGenerated,
                KnownCoreCompilerFacts.AstParsed,
                KnownCoreCompilerFacts.AstBound,
                KnownCoreCompilerFacts.BytecodeGenerated),

            CompilerPipelineStage.Air or CompilerPipelineStage.OptimizedAir => Create(
                KnownCoreCompilerFacts.SourceAvailable,
                KnownCoreCompilerFacts.LexemesGenerated,
                KnownCoreCompilerFacts.AstParsed,
                KnownCoreCompilerFacts.AstBound,
                KnownCoreCompilerFacts.BytecodeGenerated,
                KnownCoreCompilerFacts.BytecodeVerified,
                KnownCoreCompilerFacts.AirGenerated),

            CompilerPipelineStage.BackendInput => Create(
                KnownCoreCompilerFacts.SourceAvailable,
                KnownCoreCompilerFacts.LexemesGenerated,
                KnownCoreCompilerFacts.AstParsed,
                KnownCoreCompilerFacts.AstBound,
                KnownCoreCompilerFacts.BytecodeGenerated,
                KnownCoreCompilerFacts.BytecodeVerified,
                KnownCoreCompilerFacts.AirGenerated,
                KnownCoreCompilerFacts.AirVerified),

            _ => CompilerFactState.Empty
        };

    private static CompilerFactState Create(params CompilerFactId[] facts) =>
        new(facts.ToHashSet(), new HashSet<CompilerFactId>());
}
