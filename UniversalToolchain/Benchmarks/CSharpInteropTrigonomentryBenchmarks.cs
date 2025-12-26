using BasicStdLib;
using BenchmarkDotNet.Attributes;
using IntermediateRepresentationAbstractions;
using WhitespacesModule;

namespace Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class CSharpInteropTrigonomentryBenchmarks
{
    private readonly string _trigonometry = @"
        let angle = 0.5
        let sinVal = Main.Sin(angle)
        let cosVal = Main.Cos(angle)
        let tanVal = sinVal / cosVal
        sinVal * sinVal + cosVal * cosVal";

    private ICoreOptimizedRunnable _compilerCore = null!;
    private ICoreOptimizedRunnable _interpreterCore = null!;

    [GlobalSetup]
    public void Setup()
    {
        Main.LoadStdLibToThisAssembly();

        var modulesWithCSharp = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new SemicolonAsNewLineModuleImpl(),
            new ArithmeticModuleImpl(),
            new CSharpInteropModuleImpl(),
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
            modulesWithCSharp,
            []
        );

        _compilerCore = new BasicCoreImpl<DynamicMethod>(
            () => new BasicLexerImpl(),
            () => new BasicParserImpl(),
            () => new BasicBytecodeTranslatorImpl(),
            () => new AbstractMethodsCompilerImpl(),
            () => new DynamicMethodExecutor(),
            modulesWithCSharp,
            []
        );
    }

    [Benchmark]
    public object? Interpreter_Trigonometry()
    {
        _interpreterCore.PrepareToRun(_trigonometry);
        return _interpreterCore.RunPrepared();
    }

    [Benchmark(Baseline = true)]
    public object? Compiler_Trigonometry()
    {
        _compilerCore.PrepareToRun(_trigonometry);
        return _compilerCore.RunPrepared();
    }
}