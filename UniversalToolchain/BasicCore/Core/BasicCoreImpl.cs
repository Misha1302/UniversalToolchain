using BasicCore.Capabilities;

namespace BasicCore.Core;

public class BasicCoreImpl<TCompilationOutput>(
    Func<ILexer> lexerFactory,
    Func<IParser> parserFactory,
    Func<IAstToBytecodeTranslator> astTranslatorFactory,
    Func<IAbstractMethodsTranslator> abstractMethodsTranslatorFactory,
    Func<IAbstractIrCompiler<TCompilationOutput>> compilerFactory,
    Func<IExecutor<TCompilationOutput>> executorFactory,
    IReadOnlyList<IFrontendCoreModule> modules,
    IReadOnlyList<IAirOptimizer> optimizers,
    IReadOnlyList<IMiddleEndCoreModule<TCompilationOutput>> middleEndModules,
    IIntrinsicCapabilitySetFactory? intrinsicCapabilitySetFactory = null,
    IReadOnlyList<ICompilationPipelineObserver>? pipelineObservers = null,
    IReadOnlyList<IBackendPipelineComponent>? backendComponents = null
) : ICoreRunnable, ICoreOptimizedRunnable, IExecutableGiver<TCompilationOutput>, IArtifactCompiler<TCompilationOutput>
{
    private readonly CompilationInputNormalizer _inputNormalizer = new();

    private readonly AsyncLocal<PreparedExecution<TCompilationOutput>?> _prepared = new();

    private readonly PreparedExecutionBuilder<TCompilationOutput> _preparedExecutionBuilder =
        new(
            lexerFactory,
            parserFactory,
            astTranslatorFactory,
            abstractMethodsTranslatorFactory,
            compilerFactory,
            executorFactory,
            modules,
            optimizers,
            middleEndModules,
            intrinsicCapabilitySetFactory,
            pipelineObservers,
            backendComponents);

    public ICompiledArtifact<TCompilationOutput> Compile(string code, OrderedDictionary<string, Type>? parameters = null)
        => Compile(_inputNormalizer.NormalizeDeclaredInput(code, parameters));

    public ICompiledArtifact<TCompilationOutput> Compile(CompilationInput input)
        => _preparedExecutionBuilder.Compile(input);

    ICompiledArtifact IArtifactCompiler.Compile(string code, OrderedDictionary<string, Type>? parameters)
        => Compile(code, parameters);

    ICompiledArtifact IArtifactCompiler.Compile(CompilationInput input)
        => Compile(input);

    public void PrepareToRun(string code, OrderedDictionary<string, Type>? parameters = null)
    {
        PrepareToRun(_inputNormalizer.NormalizeDeclaredInput(code, parameters));
    }

    public object? RunPrepared()
    {
        var prepared = _prepared.Value;
        Thrower.AssertAlways(prepared != null);
        return prepared.Session.Run();
    }

    public object? Run(string code, Dictionary<string, object>? parameters = null)
    {
        PrepareToRun(_inputNormalizer.NormalizeRuntimeInput(code, parameters));

        return RunPrepared();
    }

    public TCompilationOutput GetExecutable(string code, OrderedDictionary<string, Type>? parameters = null)
        => Compile(code, parameters).CompilationOutput;

    public void PrepareToRun(CompilationInput input)
    {
        // A failed build must invalidate the previously prepared execution. Keeping the old
        // session would make RunPrepared execute a different program than the caller requested.
        _prepared.Value = null;
        _prepared.Value = _preparedExecutionBuilder.Build(input);
    }
}
