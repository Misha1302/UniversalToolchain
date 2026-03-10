namespace Tests.Core;

[TestFixture]
public class InterpreterEnvironmentBugTests
{
    [Test]
    public void InterpreterCore_Run_WithExternalParameter_UsesExecutionEnvironmentValue()
    {
        using var provider = CreateProvider() as ServiceProvider;
        var core = CreateInterpreterCore(provider!);

        var result = core.Run("a", new Dictionary<string, object> { ["a"] = 5 });

        Assert.That(result, Is.EqualTo(5),
            "Interpreter execution path should honor runtime external values from the execution environment.");
    }

    private static IServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistServices(options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);
        return services.BuildServiceProvider();
    }

    private static BasicCoreImpl<IAbstractIR> CreateInterpreterCore(IServiceProvider provider)
    {
        var modules = provider.GetServices<IFrontendCoreModule>().ToList();
        var optimizers = provider.GetServices<IIRProcessingModule>().ToList();

        return new BasicCoreImpl<IAbstractIR>(
            provider.GetRequiredService<Func<ILexer>>(),
            provider.GetRequiredService<Func<IParser>>(),
            provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
            provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
            () => provider.GetRequiredService<AbstractIrToAbstractIrStub>(),
            () => new InterpreterImpl(),
            modules,
            optimizers,
            []);
    }
}
