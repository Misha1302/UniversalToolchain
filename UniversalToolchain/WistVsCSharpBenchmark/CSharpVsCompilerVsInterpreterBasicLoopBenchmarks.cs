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

    private BasicCoreImpl<DynamicMethod> _compilerCore = null!;
    private BasicCoreImpl<DynamicMethod> _compilerNotOptimizedCore = null!;
    private BasicCoreImpl<IAbstractIR> _interpreterCore = null!;


    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection().AddWistServices("./../../../../../../../..");

        var provider = services.BuildServiceProvider();
        _compilerCore = provider.GetService<BasicCoreImpl<DynamicMethod>>().NotNull();
        _interpreterCore = provider.GetService<BasicCoreImpl<IAbstractIR>>().NotNull();

        services.Remove(services.First(x => x.ImplementationType == typeof(LocalVariablesOptimizer)));
        var providerNotOptimized = services.BuildServiceProvider();
        _compilerNotOptimizedCore = providerNotOptimized.GetService<BasicCoreImpl<DynamicMethod>>().NotNull();

        _compilerCore.PrepareToRun(_loopSum);
        _compilerNotOptimizedCore.PrepareToRun(_loopSum);
        _interpreterCore.PrepareToRun(_loopSum);
    }

    [Benchmark]
    public object? Interpreter_BasicLoop() => _interpreterCore.RunPrepared();

    [Benchmark]
    public object? Compiler_BasicLoop() => _compilerNotOptimizedCore.RunPrepared();

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public object NativeCSharp_NoOptimizations_BasicLoop()
    {
        var sum = new RealNumberImpl(0);
        var i = new RealNumberImpl(1);

        loop:
        if (Comparisons.Greater(i, new RealNumberImpl(100))) goto end;
        {
            sum = RealNumberImpl.Add(sum, i);
            i = RealNumberImpl.Add(i, new RealNumberImpl(1));
            goto loop;
        }
        end:
        return sum;
    }

    [Benchmark]
    public object? CompilerOptimized_BasicLoop() => _compilerCore.RunPrepared();

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    public object NativeCSharp_Optimized_BasicLoop()
    {
        var sum = new RealNumberImpl(0);
        var i = new RealNumberImpl(1);

        loop:
        if (Comparisons.Greater(i, new RealNumberImpl(100))) goto end;
        {
            sum = RealNumberImpl.Add(sum, i);
            i = RealNumberImpl.Add(i, new RealNumberImpl(1));
            goto loop;
        }
        end:
        return sum;
    }
}