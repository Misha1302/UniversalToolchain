using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using AbstractIrConverters;
using ArithmeticModule;
using BasicCilCompiler;
using BasicCodeTranslator;
using BasicCore;
using BasicInterpreter;
using BasicLexer;
using BasicParser;
using BasicStdLib;
using BenchmarkDotNet.Attributes;
using BytecodeDynamicMethodsCompiler;
using ConditionsModule;
using EqualityModule;
using IdentifierModule;
using IntermediateRepresentationAbstractions;
using LabelsModule;
using LocalVariablesOptimizerModule;
using NumbersModule;
using ScopesModule;
using SemicolonAsNewLineModule;
using VariablesModule;
using WhitespacesModule;

namespace WistVsCSharp;

[MemoryDiagnoser]
[RankColumn]
public class CSharpVsCompilerVsInterpreterBasicLoopBenchmarks
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
    private ICoreOptimizedRunnable _compilerOptimizedCore = null!;
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
            () => new BasicAstToBytecodeTranslatorImpl(),
            () => new BytecodeToAbstractIrConverterImpl(),
            () => new AbstractIrToAbstractIrStub(),
            () => new InterpreterImpl(),
            commonModules,
            []
        );

        _compilerCore = new BasicCoreImpl<DynamicMethod>(
            () => new BasicLexerImpl(),
            () => new BasicParserImpl(),
            () => new BasicAstToBytecodeTranslatorImpl(),
            () => new BytecodeToAbstractIrConverterImpl(),
            () => new AbstractMethodsCompilerImpl(),
            () => new DynamicMethodExecutor(),
            commonModules,
            []
        );

        _compilerOptimizedCore = new BasicCoreImpl<DynamicMethod>(
            () => new BasicLexerImpl(),
            () => new BasicParserImpl(),
            () => new BasicAstToBytecodeTranslatorImpl(),
            () => new BytecodeToAbstractIrConverterImpl(),
            () => new AbstractMethodsCompilerImpl(),
            () => new DynamicMethodExecutor(),
            commonModules.Union([new LocalVariablesOptimizer()]).ToList(),
            []
        );

        _interpreterCore.PrepareToRun(_loopSum);
        _compilerCore.PrepareToRun(_loopSum);
        _compilerOptimizedCore.PrepareToRun(_loopSum);
    }

    [Benchmark]
    public object? Interpreter_BasicLoop()
    {
        return _interpreterCore.RunPrepared();
    }

    [Benchmark]
    public object? Compiler_BasicLoop()
    {
        return _compilerCore.RunPrepared();
    }

    [Benchmark]
    public object? CompilerOptimized_BasicLoop()
    {
        return _compilerOptimizedCore.RunPrepared();
    }

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public object NativeCSharp_BasicLoop()
    {
        var sum = 0;
        var i = 1;

        loop:
        if (i > 100) goto end;
        {
            sum += i;
            i += 1;
            goto loop;
        }
        end:
        return sum;
    }
}