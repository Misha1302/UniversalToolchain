namespace UniversalToolchain.ModuleContracts;

public interface ICompilerStageFactSeedProvider
{
    CompilerFactState CreateInitialState(CompilerPipelineStage stage);
}
