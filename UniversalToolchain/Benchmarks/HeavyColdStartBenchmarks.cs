using BasicStdLib;
using BenchmarkDotNet.Attributes;
using WhitespacesModule;

namespace Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class HeavyColdStartBenchmarks
{
    private ICoreOptimizedRunnable _interpreterCore = null!;
    private ICoreOptimizedRunnable _compilerCore = null!;

    private readonly string _heavyLoop = @"
        let sum = 0
        let i = 1
        @loop:
        if i > 2 goto @end
            let j = 1
            @inner:
            if j > 2 goto @inner_end
                sum = sum + (i * j)
                j = j + 1
                goto @inner
            @inner_end:
            i = i + 1
            goto @loop
        @end:
        sum";

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

        _interpreterCore = new BasicCoreImpl<AbstractIR>(
            () => new BasicLexerImpl(),
            () => new BasicParserImpl(),
            () => new BasicBytecodeTranslatorImpl(),
            () => new AbstractMethodsStubImpl(),
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
    
    [Benchmark(Baseline = true)]
    public object? Interpreter_ColdStart()
    {
        _interpreterCore.PrepareToRun("");
        _interpreterCore.PrepareToRun(_heavyLoop);
        return _interpreterCore.RunPrepared();
    }                           
                                                                                                                    
    [Benchmark]
    public object? Compiler_ColdStart()
    {
        _compilerCore.PrepareToRun("");
        _compilerCore.PrepareToRun(_heavyLoop);
        return _compilerCore.RunPrepared();
    }
}