namespace BasicCore.Contracts;

/// <summary>
///     Observes stable compilation boundaries without making BasicCore depend on feature-specific validators.
/// </summary>
public interface ICompilationPipelineObserver
{
    void AfterBytecode(CompilationPipelineBytecodeContext context)
    {
    }

    void AfterAir(CompilationPipelineAirContext context)
    {
    }

    void AfterOptimizedAir(CompilationPipelineAirContext context)
    {
    }
}

public sealed record CompilationPipelineBytecodeContext(
    CompilationInput Input,
    IReadOnlyList<IFrontendCoreModule> FrontendModules,
    Bytecode Bytecode,
    IReadOnlyList<IBackendPipelineComponent>? BackendComponents = null);

public sealed record CompilationPipelineAirContext(
    CompilationInput Input,
    IReadOnlyList<IFrontendCoreModule> FrontendModules,
    IReadOnlyList<IAirOptimizer> Optimizers,
    IAbstractIR Air,
    IReadOnlyList<string> CompilerSupportedIntrinsics,
    IReadOnlyList<IBackendPipelineComponent>? BackendComponents = null);
