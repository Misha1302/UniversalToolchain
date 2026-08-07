using BasicCore.Capabilities;
using BasicCore.Ir;

namespace BasicCore.Core;

internal sealed class PreparedExecutionBuilder<TCompilationOutput>(
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
    IReadOnlyList<IBackendPipelineComponent>? backendComponents = null)
{
    private readonly IReadOnlyList<ICompilationPipelineObserver> _pipelineObservers = pipelineObservers ?? [];
    private readonly IReadOnlyList<IBackendPipelineComponent> _backendComponents = backendComponents ?? [];

    public ICompiledArtifact<TCompilationOutput> Compile(CompilationInput input)
    {
        var buildResult = BuildCompilationResult(input);

        return new CompiledArtifact<TCompilationOutput>(
            buildResult.SourceText,
            buildResult.DeclaredBindings,
            buildResult.CompilationOutput,
            buildResult.Executor,
            buildResult.AllowedRuntimeProviderTypes);
    }

    public PreparedExecution<TCompilationOutput> Build(CompilationInput input)
    {
        var buildResult = BuildCompilationResult(input);
        var artifact = new CompiledArtifact<TCompilationOutput>(
            buildResult.SourceText,
            buildResult.DeclaredBindings,
            buildResult.CompilationOutput,
            buildResult.Executor,
            buildResult.AllowedRuntimeProviderTypes);
        var session = artifact.CreateSession();

        return new PreparedExecution<TCompilationOutput>(
            artifact.SourceText,
            artifact,
            session);
    }

    private CompilationBuildResult<TCompilationOutput> BuildCompilationResult(CompilationInput input)
    {
        input = input.ArgNotNull();

        var lexer = lexerFactory();
        var parser = parserFactory();
        var astTranslator = astTranslatorFactory();
        var methodsTranslator = abstractMethodsTranslatorFactory();
        var compiler = compilerFactory();
        var executor = executorFactory();
        var intrinsicCapabilitySet = (intrinsicCapabilitySetFactory ?? new CompilerIntrinsicCapabilitySetFactory()).Create(compiler);
        var optimizerCapabilityContext = new OptimizerIntrinsicCapabilityContext(intrinsicCapabilitySet);

        var boundRoot = CanonicalArtifactStages.ParseAndBind(input, lexer, parser, modules);
        var targetBytecode = CanonicalArtifactStages.LowerToBytecode(boundRoot, astTranslator, modules);
        NotifyAfterBytecode(input, targetBytecode);

        optimizers.ForEach(module => module.InitMethodsTranslator(methodsTranslator));
        optimizers.ForEach(module => module.InitIntrinsicCapabilityContext(optimizerCapabilityContext));
        var air = CanonicalArtifactStages.LowerToAir(targetBytecode, methodsTranslator);
        NotifyAfterAir(input, air, compiler.SupportedIntrinsics);

        var irPipeline = new AirOnlyIrPipelineExecutor(optimizers);
        var targetIr = irPipeline.Optimize(air);
        NotifyAfterOptimizedAir(input, targetIr, compiler.SupportedIntrinsics);
        var allowedRuntimeProviderTypes = ResolveAllowedRuntimeProviderTypes();
        middleEndModules.ForEach(module => module.InitMethodsCompiler(compiler));
        var compiled = compiler.Compile(targetIr, input);

        var compilationOutput = middleEndModules.Aggregate(compiled, (current, module) => module.ProcessCompilation(current));
        middleEndModules.ForEach(module => module.InitExecutor(executor));

        return new CompilationBuildResult<TCompilationOutput>(
            input.SourceText,
            input.ExternalBindings,
            compilationOutput,
            executor,
            allowedRuntimeProviderTypes);
    }

    private void NotifyAfterBytecode(CompilationInput input, Bytecode bytecode)
    {
        if (_pipelineObservers.Count == 0)
            return;

        var context = new CompilationPipelineBytecodeContext(input, modules, bytecode, _backendComponents);
        foreach (var observer in _pipelineObservers)
            observer.AfterBytecode(context);
    }

    private void NotifyAfterAir(
        CompilationInput input,
        IAbstractIR air,
        IReadOnlyList<string> compilerSupportedIntrinsics)
    {
        if (_pipelineObservers.Count == 0)
            return;

        var context = new CompilationPipelineAirContext(
            input,
            modules,
            optimizers,
            air,
            compilerSupportedIntrinsics,
            _backendComponents);
        foreach (var observer in _pipelineObservers)
            observer.AfterAir(context);
    }

    private void NotifyAfterOptimizedAir(
        CompilationInput input,
        IAbstractIR air,
        IReadOnlyList<string> compilerSupportedIntrinsics)
    {
        if (_pipelineObservers.Count == 0)
            return;

        var context = new CompilationPipelineAirContext(
            input,
            modules,
            optimizers,
            air,
            compilerSupportedIntrinsics,
            _backendComponents);
        foreach (var observer in _pipelineObservers)
            observer.AfterOptimizedAir(context);
    }

    private IReadOnlyList<Type> ResolveAllowedRuntimeProviderTypes() =>
        _backendComponents
            .OfType<IRuntimeProviderPolicyComponent>()
            .SelectMany(static component => component.AllowedRuntimeProviderTypes)
            .Distinct()
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();
}

internal sealed class CompilationBuildResult<TCompilationOutput>(
    string sourceText,
    IReadOnlyList<ExternalBinding> declaredBindings,
    TCompilationOutput compilationOutput,
    IExecutor<TCompilationOutput> executor,
    IReadOnlyList<Type> allowedRuntimeProviderTypes)
{
    public string SourceText { get; } = sourceText;

    public IReadOnlyList<ExternalBinding> DeclaredBindings { get; } = declaredBindings;

    public TCompilationOutput CompilationOutput { get; } = compilationOutput;

    public IExecutor<TCompilationOutput> Executor { get; } = executor;

    public IReadOnlyList<Type> AllowedRuntimeProviderTypes { get; } = allowedRuntimeProviderTypes;
}
