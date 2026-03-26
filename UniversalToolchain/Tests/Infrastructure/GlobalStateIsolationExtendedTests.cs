using DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;

namespace Tests.Infrastructure;

[TestFixture]
public class GlobalStateIsolationExtendedTests
{
    [Test]
    public void RepeatedSuccessfulRuns_OnSameProvider_ShouldNotAccumulateExecutionState()
    {
        using var provider = BuildProvider();
        var compiler = GetCore(provider, isCompiler: true);

        var baseline = ToDouble(compiler.Run("let x = 5\nlet y = 2\nx * y"));
        for (var i = 0; i < 30; i++)
        {
            var value = ToDouble(compiler.Run("let x = 5\nlet y = 2\nx * y"));
            Assert.That(value, Is.EqualTo(baseline).Within(1e-9));
        }
    }

    [Test]
    public void FailedExecution_ShouldNotAffectNextSuccessfulExecution_OnSameProvider()
    {
        using var provider = BuildProvider();
        var interpreter = GetCore(provider, isCompiler: false);

        Assert.Catch(() => interpreter.Run("let broken ="));
        var value = ToDouble(interpreter.Run("40 + 2"));

        Assert.That(value, Is.EqualTo(42).Within(1e-9));
    }

    [Test]
    public void SuccessThenFailThenSuccess_ShouldRemainStable_OnSameProvider()
    {
        using var provider = BuildProvider();
        var interpreter = GetCore(provider, isCompiler: false);

        var first = ToDouble(interpreter.Run("21 + 21"));
        Assert.Catch(() => interpreter.Run("let broken ="));
        var second = ToDouble(interpreter.Run("21 + 21"));

        Assert.That(second, Is.EqualTo(first).Within(1e-9));
    }

    [Test]
    public void SameProvider_ShouldKeepSameVariableNamesSeparated_AcrossDistinctPrograms()
    {
        using var provider = BuildProvider();
        var interpreter = GetCore(provider, isCompiler: false);

        var first = ToDouble(interpreter.Run("let x = 10\nx + 1"));
        var second = ToDouble(interpreter.Run("let x = 3\nx * 2"));

        Assert.That(first, Is.EqualTo(11).Within(1e-9));
        Assert.That(second, Is.EqualTo(6).Within(1e-9));
    }

    [Test]
    public void InterleavedExecutions_ShouldKeepRequestSpecificStateSeparated()
    {
        using var provider = BuildProvider();
        var compiler = GetCore(provider, isCompiler: true);
        var interpreter = GetCore(provider, isCompiler: false);

        for (var i = 1; i <= 15; i++)
        {
            var codeA = $"let a = {i}\na + {i}";
            var codeB = $"let b = {i * 2}\nb - {i}";

            Assert.That(ToDouble(compiler.Run(codeA)), Is.EqualTo(i * 2).Within(1e-9));
            Assert.That(ToDouble(interpreter.Run(codeB)), Is.EqualTo(i).Within(1e-9));
        }
    }


    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddWistServices();
        return services.BuildServiceProvider();
    }

    private static ICoreRunnable GetCore(ServiceProvider provider, bool isCompiler)
    {
        var targetType = isCompiler ? typeof(DynamicMethod) : typeof(IAbstractIR);
        return provider.GetServices<ICoreRunnable>()
            .First(x => x.GetType().IsGenericType && x.GetType().GetGenericArguments()[0] == targetType);
    }

    private static double ToDouble(object? value)
    {
        return value switch
        {
            RealNumberImpl number => number.GetValue(),
            int x => x,
            long x => x,
            float x => x,
            double x => x,
            decimal x => (double)x,
            _ => throw new InvalidOperationException($"Unsupported numeric result '{value?.GetType().FullName ?? "<null>"}'.")
        };
    }
}
