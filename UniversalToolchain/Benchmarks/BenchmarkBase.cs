using BenchmarkDotNet.Attributes;
using DependencyInjection;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks;

public abstract class BenchmarkBase
{
    protected BasicCoreImpl<DynamicMethod> CompilerCore = null!;
    protected BasicCoreImpl<IAbstractIR> InterpreterCore = null!;


    [GlobalSetup]
    public void Setup()
    {
        var provider = new ServiceCollection()
            .AddWistServices()
            .BuildServiceProvider();

        CompilerCore = provider.GetService<BasicCoreImpl<DynamicMethod>>().NotNull();
        InterpreterCore = provider.GetService<BasicCoreImpl<IAbstractIR>>().NotNull();
    }
}