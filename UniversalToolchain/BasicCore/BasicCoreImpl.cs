using System.Text;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using ExceptionsManager;

namespace BasicCore;

public class BasicCoreImpl<TCompilationOutput>(
    Func<ILexer> lexerFactory,
    Func<IParser> parserFactory,
    Func<IAstToBytecodeTranslator> astTranslatorFactory,
    Func<IAbstractMethodsTranslator> abstractMethodsTranslatorFactory,
    Func<IAbstractIrCompiler<TCompilationOutput>> compilerFactory,
    Func<IExecutor<TCompilationOutput>> executorFactory,
    IReadOnlyList<IFrontendCoreModule> modules,
    IReadOnlyList<IIRProcessingModule> optimizers,
    IReadOnlyList<IMiddleEndCoreModule<TCompilationOutput>> middleEndModules,
    Func<string, OrderedDictionary<string, Type>, string>? codeWithParamsFactory = null
) : ICoreRunnable, ICoreOptimizedRunnable, IExecutableGiver<TCompilationOutput>
{
    private string _code = null!;
    private TCompilationOutput _compilationOutput = default!;
    private IExecutor<TCompilationOutput> _executor = null!;

    public void PrepareToRun(string code, OrderedDictionary<string, Type>? parameters = null)
    {
        parameters ??= [];

        code = (codeWithParamsFactory ?? GetCodeWithParametersDefault)(code, parameters);

        var lexer = lexerFactory();
        var parser = parserFactory();
        var astTranslator = astTranslatorFactory();
        var methodsTranslator = abstractMethodsTranslatorFactory();
        var compiler = compilerFactory();
        var executor = executorFactory();

        var targetCode = modules.Aggregate(code, (current, module) => module.ProcessText(current));
        modules.ForEach(module => module.InitLexer(lexer));
        var lexemes = lexer.Lexemize(targetCode);

        var targetLexemes = modules.Aggregate(lexemes, (current, module) => module.ProcessLexemes(current));
        modules.ForEach(module => module.InitParser(parser));
        var astRoot = parser.Parse(targetLexemes);

        var targetRoot = modules.Aggregate(astRoot, (current, module) => module.ProcessAst(current));
        modules.ForEach(module => module.InitAstTranslator(astTranslator));
        var bytecode = astTranslator.Translate(targetRoot);

        var targetBytecode = modules.Aggregate(bytecode, (current, module) => module.ProcessBytecode(current));
        optimizers.ForEach(module => module.InitMethodsTranslator(methodsTranslator));
        var air = methodsTranslator.Translate(targetBytecode);

        var targetIr = optimizers.Aggregate(air, (current, module) => module.ProcessIr(current, compiler));
        middleEndModules.ForEach(module => module.InitMethodsCompiler(compiler));
        var compiled = compiler.Compile(targetIr, parameters);

        var compilationOutput = middleEndModules.Aggregate(compiled, (current, module) => module.ProcessCompilation(current));
        middleEndModules.ForEach(module => module.InitExecutor(executor));

        _executor = executor;
        _compilationOutput = compilationOutput;
        _code = code;
    }

    public object? RunPrepared()
    {
        Thrower.AssertAlways(_code != null);
        return _executor.Execute(_compilationOutput);
    }

    public object? Run(string code, Dictionary<string, object>? parameters = null)
    {
        parameters ??= [];
        PrepareToRun(
            code,
            new OrderedDictionary<string, Type>(
                parameters.ToDictionary(
                    x => x.Key,
                    x => x.Value.GetType()
                )
            )
        );
        return RunPrepared();
    }

    public TCompilationOutput GetExecutable(string code, OrderedDictionary<string, Type>? parameters = null)
    {
        parameters ??= [];
        PrepareToRun(code, parameters);
        return _compilationOutput;
    }

    private string GetCodeWithParametersDefault(string code, OrderedDictionary<string, Type> parameters)
    {
        var sb = new StringBuilder();
        foreach (var param in parameters)
            sb.AppendLine($"#![define {param.Key} as {param.Value}]");
        sb.AppendLine();
        return sb + code;
    }
}