using BasicStdLib;
using BenchmarkDotNet.Attributes;
using IntermediateRepresentationAbstractions;
using WhitespacesModule;

namespace Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class ConditionsBenchmarks
{
    private readonly string _conditions = @"
        let x = 75
        let result = 0
        
        if x >= 90
            result = 5
        elif x >= 80
            result = 4
        elif x >= 70
            result = 3
        elif x >= 60
            result = 2
        else
            result = 1
        
        result";

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
    public object? Interpreter_Conditions()
    {
        _interpreterCore.PrepareToRun(_conditions);
        return _interpreterCore.RunPrepared();
    }

    [Benchmark(Baseline = true)]
    public object? Compiler_Conditions()
    {
        _compilerCore.PrepareToRun(_conditions);
        return _compilerCore.RunPrepared();
    }
}