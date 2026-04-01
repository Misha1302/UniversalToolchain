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

        var slotsByName = new Dictionary<string, int>(input.ExternalBindings.Count, StringComparer.Ordinal);
        for (var i = 0; i < input.ExternalBindings.Count; i++)
        {
            var binding = input.ExternalBindings[i];
            if (!slotsByName.TryAdd(binding.Name, i))
                Thrower.Argument(nameof(input), $"Declared binding '{binding.Name}' is duplicated.");
        }

        _ = slotsByName;

        return new CompiledArtifact<TCompilationOutput>(
            input.SourceText,
            input.ExternalBindings,
            compilationOutput);
    }

    public PreparedExecution<TCompilationOutput> Build(CompilationInput input)
    {
        var artifact = Compile(input);
        var session = new CompiledArtifactSession<TCompilationOutput>(
            artifact.CompilationOutput,
            executorFactory(),
            artifact.CreateSession(),
            artifact.DeclaredBindings);

        return new PreparedExecution<TCompilationOutput>(
            artifact.SourceText,
            artifact,
            session);
    }
}
