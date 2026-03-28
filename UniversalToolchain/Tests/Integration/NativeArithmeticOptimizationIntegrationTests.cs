namespace Tests.Integration;

public class NativeArithmeticOptimizationIntegrationTests
{
    [Test]
    public void NativePipeline_ShouldOptimizeStraightLineArithmeticAndKeepExecutionCorrect()
    {
        var services = new ServiceCollection();
        services.AddWistServices(options => options.ArithmeticMode = ArithmeticMode.Native);
        using var provider = services.BuildServiceProvider();

        var methodGiver = provider.GetRequiredService<IExecutableGiver<DynamicMethod>>();

        const string code = "(a + 0) * 1 + ((a + 2) + 3)";
        var bindings = new OrderedDictionary<string, Type> { { "a", typeof(int) } };

        var dynamicMethod = methodGiver.GetExecutable(code, bindings);
        var invoker = new DynamicMethodInvoker<int, int>(dynamicMethod);

        var result = invoker.Invoke(7);

        Assert.That(result, Is.EqualTo(19));
    }
}