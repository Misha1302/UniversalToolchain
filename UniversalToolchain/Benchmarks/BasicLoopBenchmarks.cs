using BasicStdLib;
using BenchmarkDotNet.Attributes;
using IntermediateRepresentationAbstractions;
using WhitespacesModule;

namespace Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class BasicLoopBenchmarks
{
    private readonly string _loopSum = @"
        let sum = 0
        let i = 1
        @loop:
        if i > 100 goto @end
            sum = sum + i
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

        var commonModules = new IFrontendCoreModule[]
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
            commonModules,
            []
        );

        _compilerCore = new BasicCoreImpl<DynamicMethod>(
            () => new BasicLexerImpl(),
            () => new BasicParserImpl(),
            () => new BasicBytecodeTranslatorImpl(),
            () => new AbstractMethodsCompilerImpl(),
            () => new DynamicMethodExecutor(),
            commonModules,
            []
        );
    }

    [Benchmark]
    public object? Interpreter_BasicLoop()
    {
        _interpreterCore.PrepareToRun(_loopSum);
        return _interpreterCore.RunPrepared();
    }

    [Benchmark(Baseline = true)]
    public object? Compiler_BasicLoop()
    {
        _compilerCore.PrepareToRun(_loopSum);
        return _compilerCore.RunPrepared();
    }
}