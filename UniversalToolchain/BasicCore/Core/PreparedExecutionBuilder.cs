using BasicCore.Capabilities;

namespace BasicCore.Core;

internal sealed class PreparedExecutionBuilder<TCompilationOutput>(
    Func<ILexer> lexerFactory,
    Func<IParser> parserFactory,
    Func<IAstToBytecodeTranslator> astTranslatorFactory,
    Func<IAbstractMethodsTranslator> abstractMethodsTranslatorFactory,
    Func<IAbstractIrCompiler<TCompilationOutput>> compilerFactory,
    Func<IExecutor<TCompilationOutput>> executorFactory,
    IReadOnlyList<IFrontendCoreModule> modules,
    IReadOnlyList<IIRProcessingModule> optimizers,
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

        var targetCode = modules.Aggregate(input.SourceText, (current, module) => module.ProcessText(current));
        modules.ForEach(module => module.InitLexer(lexer));
        var lexemes = lexer.Lexemize(targetCode);

        var targetLexemes = modules.Aggregate(lexemes, (current, module) => module.ProcessLexemes(current));
        modules.ForEach(module => module.InitParser(parser));
        var astRoot = parser.Parse(targetLexemes);

        var targetRoot = modules.Aggregate(astRoot, (current, module) => module.ProcessAst(current));
        var boundRoot = new Binder(input.ExternalBindings).Bind(targetRoot);

        modules.ForEach(module => module.InitAstTranslator(astTranslator, modules));
        var bytecode = astTranslator.Translate(boundRoot);

        var targetBytecode = modules.Aggregate(bytecode, (current, module) => module.ProcessBytecode(current));
        NotifyAfterBytecode(input, targetBytecode);
        optimizers.ForEach(module => module.InitMethodsTranslator(methodsTranslator));
        optimizers.ForEach(module => module.InitIntrinsicCapabilityContext(optimizerCapabilityContext));
        var air = methodsTranslator.Translate(targetBytecode);
        NotifyAfterAir(input, air, compiler.SupportedIntrinsics);

        var targetIr = optimizers.Aggregate(air, (current, module) => module.ProcessIr(current, compiler));
        NotifyAfterOptimizedAir(input, targetIr, compiler.SupportedIntrinsics);
        var allowedRuntimeProviderTypes = ExtractAllowedRuntimeProviderTypes(targetIr);
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

    private static IReadOnlyList<Type> ExtractAllowedRuntimeProviderTypes(IAbstractIR ir)
    {
        var providers = new HashSet<Type>();
        foreach (var instruction in ir.Instructions)
        {
            if (instruction.UOpCode != UOpCode.Intrinsic || instruction.Operands.Count < 2)
                continue;

            if (!Equals(instruction.Operands[0], "call C#"))
                continue;

            if (instruction.Operands[1] is not CSharpCallDescriptor descriptor)
                continue;

            if (descriptor.Receiver is CSharpCallReceiver.ExecutionScopedProvider executionScopedProvider)
                providers.Add(executionScopedProvider.ProviderType);
        }

        return providers.ToList();
    }
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
