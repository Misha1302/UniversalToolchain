using BasicStdLib;
using BenchmarkDotNet.Attributes;
using IntermediateRepresentationAbstractions;
using WhitespacesModule;

namespace Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class CSharpInteropMathBenchmarks
{
    private readonly string _mathFunctions = @"
        let x = 2.5
        let result = Main.Sqrt(x) + Main.Log(x, 10) + Main.Pow(x, 2)
        result";

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
            () => new AbstractMethodsStubImpl(),
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
    public object? Interpreter_MathFunctions()
    {
        _interpreterCore.PrepareToRun(_mathFunctions);
        return _interpreterCore.RunPrepared();
    }

    [Benchmark(Baseline = true)]
    public object? Compiler_MathFunctions()
    {
        _compilerCore.PrepareToRun(_mathFunctions);
        return _compilerCore.RunPrepared();
    }
}