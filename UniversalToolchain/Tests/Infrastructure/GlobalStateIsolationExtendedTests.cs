using DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Infrastructure;

[TestFixture]
public class GlobalStateIsolationExtendedTests
{
    [Test]
    public void RepeatedRuns_ShouldNotAccumulateMutableExecutionState()
    {
        using var provider = BuildProvider();
        var core = GetInterpreter(provider);

        const string code = "let x = 1\nlet y = 2\nx + y";
        var first = core.Run(code);
        for (var i = 0; i < 30; i++)
        {
            Assert.That(core.Run(code), Is.EqualTo(first));
        }
    }

    [Test]
    public void FailedExecution_ShouldNotAffectNextSuccessfulExecution()
    {
        using var provider = BuildProvider();
        var core = GetInterpreter(provider);

        Assert.Catch(() => core.Run("let x ="));
        var value = core.Run("let a = 40\nlet b = 2\na + b");

        Assert.That(ToDouble(value), Is.EqualTo(42).Within(1e-9));
    }

    [Test]
    public void LongLivedProvider_ShouldRemainStable_AcrossManyExecutions()
    {
        using var provider = BuildProvider();
        var compiler = GetCompiler(provider);

        for (var i = 0; i < 50; i++)
        {
            var value = compiler.Run($"let n = {i}\nn + 1");
            Assert.That(ToDouble(value), Is.EqualTo(i + 1).Within(1e-9));
        }
    }

    [Test]
    public void FreshProvider_ShouldNotObserveState_FromPreviousProvider()
    {
        var first = ExecuteWithFreshProvider("let x = 5\nx + 5");
        var second = ExecuteWithFreshProvider("let x = 5\nx + 5");

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void InterleavedExecutions_ShouldKeepRequestSpecificStateSeparated()
    {
        using var provider = BuildProvider();
        var interpreter = GetInterpreter(provider);
        var compiler = GetCompiler(provider);

        for (var i = 1; i <= 20; i++)
        {
            var code = $"let x = {i}\nlet y = {i * 2}\nx + y";
            Assert.That(ToDouble(interpreter.Run(code)), Is.EqualTo(i * 3).Within(1e-9));
            Assert.That(ToDouble(compiler.Run(code)), Is.EqualTo(i * 3).Within(1e-9));
        }
    }

    private static object? ExecuteWithFreshProvider(string code)
    {
        using var provider = BuildProvider();
        return GetInterpreter(provider).Run(code);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddWistServices();
        return services.BuildServiceProvider();
    }

    private static ICoreRunnable GetInterpreter(IServiceProvider provider)
    {
        return provider.GetServices<ICoreRunnable>().First(r => r.GetType().IsGenericType && r.GetType().GetGenericArguments()[0] == typeof(IAbstractIR));
    }

    private static ICoreRunnable GetCompiler(IServiceProvider provider)
    {
        return provider.GetServices<ICoreRunnable>().First(r => r.GetType().IsGenericType && r.GetType().GetGenericArguments()[0] == typeof(DynamicMethod));
    }

    private static double ToDouble(object? value)
    {
        return value switch
        {
            int v => v,
            long v => v,
            float v => v,
            double v => v,
            decimal v => (double)v,
            NumbersModule.Core.RealNumberImpl v => v.GetValue(),
            _ => throw new InvalidOperationException($"Unsupported numeric value type: {value?.GetType().FullName ?? "<null>"}")
        };
    }
}
