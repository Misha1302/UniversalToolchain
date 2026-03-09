using BasicCore.Binding;
using BasicCore.Compilation;
using BasicCore.Contracts;
using BasicCore.Execution;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using ExceptionsManager;

namespace BasicCore.Core;

public class BasicCoreImpl<TCompilationOutput>(
    Func<ILexer> lexerFactory,
    Func<IParser> parserFactory,
    Func<IAstToBytecodeTranslator> astTranslatorFactory,
    Func<IAbstractMethodsTranslator> abstractMethodsTranslatorFactory,
    Func<IAbstractIrCompiler<TCompilationOutput>> compilerFactory,
    Func<IExecutor<TCompilationOutput>> executorFactory,
    IReadOnlyList<IFrontendCoreModule> modules,
    IReadOnlyList<IIRProcessingModule> optimizers,
    IReadOnlyList<IMiddleEndCoreModule<TCompilationOutput>> middleEndModules
) : ICoreRunnable, ICoreOptimizedRunnable, IExecutableGiver<TCompilationOutput>
{
    private PreparedExecution<TCompilationOutput>? _prepared;

    public void PrepareToRun(string code, OrderedDictionary<string, Type>? parameters = null)
    {
        PrepareToRun(new CompilationInput
        {
            SourceText = code,
            ExternalBindings = ExternalBindingsFactory.FromDeclaredTypes(parameters),
            Options = new CompilationOptions()
        });
    }

    public void PrepareToRun(CompilationInput input)
    {
        _prepared = BuildPreparedExecution(input);
    }

    public object? RunPrepared()
    {
        Thrower.AssertAlways(_prepared != null);
        return _prepared.Executor.Execute(_prepared.CompilationOutput, _prepared.ExecutionEnvironment);
    }

    public object? Run(string code, Dictionary<string, object>? parameters = null)
    {
        PrepareToRun(new CompilationInput
        {
            SourceText = code,
            ExternalBindings = ExternalBindingsFactory.FromRuntimeValues(parameters),
            Options = new CompilationOptions()
        });

        return RunPrepared();
    }

    public TCompilationOutput GetExecutable(string code, OrderedDictionary<string, Type>? parameters = null)
    {
        PrepareToRun(code, parameters);
        return _prepared!.CompilationOutput;
    }

    private PreparedExecution<TCompilationOutput> BuildPreparedExecution(CompilationInput input)
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

        return new PreparedExecution<TCompilationOutput>(
            input.SourceText,
            compilationOutput,
            executor,
            new ExecutionEnvironment(input.ExternalBindings));
    }
}
