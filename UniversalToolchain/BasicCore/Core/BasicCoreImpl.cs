using BasicCore.Compilation;
using BasicCore.Contracts;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;

using System.Threading;

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
    private readonly CompilationInputNormalizer _inputNormalizer = new();

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
            middleEndModules);

    private readonly AsyncLocal<PreparedExecution<TCompilationOutput>?> _prepared = new();

    public void PrepareToRun(string code, OrderedDictionary<string, Type>? parameters = null)
    {
        PrepareToRun(_inputNormalizer.NormalizeDeclaredInput(code, parameters));
    }

    public object? RunPrepared()
    {
        var prepared = _prepared.Value;
        Thrower.AssertAlways(prepared != null);
        return prepared.Executor.Execute(prepared.CompilationOutput, prepared.ExecutionEnvironment);
    }

    public object? Run(string code, Dictionary<string, object>? parameters = null)
    {
        PrepareToRun(_inputNormalizer.NormalizeRuntimeInput(code, parameters));

        return RunPrepared();
    }

    public TCompilationOutput GetExecutable(string code, OrderedDictionary<string, Type>? parameters = null)
    {
        PrepareToRun(code, parameters);
        return _prepared.Value!.CompilationOutput;
    }

    public void PrepareToRun(CompilationInput input)
    {
        _prepared.Value = _preparedExecutionBuilder.Build(input);
    }
}