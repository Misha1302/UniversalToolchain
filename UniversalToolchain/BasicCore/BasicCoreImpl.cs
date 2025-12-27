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
    IReadOnlyList<IMiddleEndCoreModule<TCompilationOutput>> middleEndModules
) : ICoreRunnable, ICoreOptimizedRunnable
{
    private string _code = null!;
    private IExecutor<TCompilationOutput> _executor = null!;
    private TCompilationOutput _targetDynamicMethods = default!;

    public void PrepareToRun(string code)
    {
        if (_code == code)
            return;

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
        modules.ForEach(module => module.InitMethodsTranslator(methodsTranslator));
        var air = methodsTranslator.Translate(targetBytecode);

        var targetIr = modules.Aggregate(air, (current, module) => module.ProcessIr(current));
        middleEndModules.ForEach(module => module.InitMethodsCompiler(compiler));
        var compiled = compiler.Compile(targetIr);

        var targetDynamicMethods = middleEndModules.Aggregate(compiled, (current, module) => module.ProcessCompilation(current));
        middleEndModules.ForEach(module => module.InitExecutor(executor));

        _executor = executor;
        _targetDynamicMethods = targetDynamicMethods;
        _code = code;
    }

    public object? RunPrepared()
    {
        Thrower.AssertAlways(_code != null);
        return _executor.Execute(_targetDynamicMethods);
    }

    public object? Run(string code)
    {
        PrepareToRun(code);
        return RunPrepared();
    }
}