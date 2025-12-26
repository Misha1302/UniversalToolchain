using BasicStdLib;
using BenchmarkDotNet.Attributes;
using IntermediateRepresentationAbstractions;
using WhitespacesModule;

namespace Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class HeavyLoopBenchmarks
{
    private readonly string _heavyLoop = @"
        let sum = 0
        let i = 1
        @loop:
        if i > 1000 goto @end
            let j = 1
            @inner:
            if j > 100 goto @inner_end
                sum = sum + (i * j)
                j = j + 1
                goto @inner
            @inner_end:
            i = i + 1
            goto @loop
        @end:
        sum";

    private ICoreOptimizedRunnable _compilerCore = null!;
    private ICoreOptimizedRunnable _interpreterCore = null!;

    [GlobalSetup]
    public void Setup()
    {
        Main.LoadStdLibToThisAssembly();

        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new SemicolonAsNewLineModuleImpl(),
            new ArithmeticModuleImpl(),
            new LabelsModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new ConditionsModuleImpl(),
            new ComparisonOperations(),
            new BooleanOperations()
        };

        _interpreterCore = new BasicCoreImpl<IAbstractIR>(
            () => new BasicLexerImpl(),
            () => new BasicParserImpl(),
            () => new BasicBytecodeTranslatorImpl(),
            () => new BytecodeToAbstractIrConverterImpl(),
            () => new InterpreterImpl(),
            modules,
            []
        );

        _compilerCore = new BasicCoreImpl<DynamicMethod>(
            () => new BasicLexerImpl(),
            () => new BasicParserImpl(),
            () => new BasicBytecodeTranslatorImpl(),
            () => new AbstractMethodsCompilerImpl(),
            () => new DynamicMethodExecutor(),
            modules,
            []
        );
    }

    // Однократное исполнение (компиляция + исполнение)
    [Benchmark]
    public object? Interpreter_HeavyLoop()
    {
        _interpreterCore.PrepareToRun(_heavyLoop);
        return _interpreterCore.RunPrepared();
    }

    [Benchmark(Baseline = true)]
    public object? Compiler_HeavyLoop()
    {
        _compilerCore.PrepareToRun(_heavyLoop);
        return _compilerCore.RunPrepared();
    }
}