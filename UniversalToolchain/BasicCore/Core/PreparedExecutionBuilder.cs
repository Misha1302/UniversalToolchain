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
    IReadOnlyList<IMiddleEndCoreModule<TCompilationOutput>> middleEndModules)
{
    public ICompiledArtifact<TCompilationOutput> Compile(CompilationInput input)
    {
        var buildResult = BuildCompilationResult(input);

        return new CompiledArtifact<TCompilationOutput>(
            buildResult.SourceText,
            buildResult.DeclaredBindings,
            buildResult.CompilationOutput,
            buildResult.Executor);
    }

    public PreparedExecution<TCompilationOutput> Build(CompilationInput input)
    {
        var buildResult = BuildCompilationResult(input);
        var artifact = new CompiledArtifact<TCompilationOutput>(
            buildResult.SourceText,
            buildResult.DeclaredBindings,
            buildResult.CompilationOutput,
            buildResult.Executor);
        var session = artifact.CreateSession();

        return new PreparedExecution<TCompilationOutput>(
            artifact.SourceText,
            artifact,
            session);
    }

    private CompilationBuildResult<TCompilationOutput> BuildCompilationResult(CompilationInput input)
    {
        if (input is null)
            Thrower.ArgumentNull(nameof(input));

        var lexer = lexerFactory();
        var parser = parserFactory();
        var astTranslator = astTranslatorFactory();
        var methodsTranslator = abstractMethodsTranslatorFactory();
        var compiler = compilerFactory();
        var executor = executorFactory();

        var targetCode = modules.Aggregate(input.SourceText, (current, module) => module.ProcessText(current));
        modules.ForEach(module => module.InitLexer(lexer));
        var lexemes = lexer.Lexemize(targetCode);

        var targetLexemes = modules.Aggregate(lexemes, (current, module) => module.ProcessLexemes(current));
        modules.ForEach(module => module.InitParser(parser));
        var astRoot = parser.Parse(targetLexemes);

        var targetRoot = modules.Aggregate(astRoot, (current, module) => module.ProcessAst(current));
        var boundRoot = new Binder(input.ExternalBindings).Bind(targetRoot);

        modules.ForEach(module => module.InitAstTranslator(astTranslator));
        var bytecode = astTranslator.Translate(boundRoot);

        var targetBytecode = modules.Aggregate(bytecode, (current, module) => module.ProcessBytecode(current));
        optimizers.ForEach(module => module.InitMethodsTranslator(methodsTranslator));
        var air = methodsTranslator.Translate(targetBytecode);

        var targetIr = optimizers.Aggregate(air, (current, module) => module.ProcessIr(current, compiler));
        middleEndModules.ForEach(module => module.InitMethodsCompiler(compiler));
        var compiled = compiler.Compile(targetIr, input);

        var compilationOutput = middleEndModules.Aggregate(compiled, (current, module) => module.ProcessCompilation(current));
        middleEndModules.ForEach(module => module.InitExecutor(executor));

        return new CompilationBuildResult<TCompilationOutput>(
            input.SourceText,
            input.ExternalBindings,
            compilationOutput,
            executor);
    }
}

internal sealed class CompilationBuildResult<TCompilationOutput>(
    string sourceText,
    IReadOnlyList<ExternalBinding> declaredBindings,
    TCompilationOutput compilationOutput,
    IExecutor<TCompilationOutput> executor)
{
    public string SourceText { get; } = sourceText;

    public IReadOnlyList<ExternalBinding> DeclaredBindings { get; } = declaredBindings;

    public TCompilationOutput CompilationOutput { get; } = compilationOutput;

    public IExecutor<TCompilationOutput> Executor { get; } = executor;
}
