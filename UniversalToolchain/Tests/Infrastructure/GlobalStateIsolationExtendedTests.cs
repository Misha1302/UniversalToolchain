using DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;

namespace Tests.Infrastructure;

[TestFixture]
public class GlobalStateIsolationExtendedTests
{
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
    public void FreshProvider_ShouldNotObserveContamination_FromPreviousProvider()
    {
        using var firstProvider = BuildProvider();
        var firstInterpreter = GetCore(firstProvider, isCompiler: false);
        var fromFirstProvider = ToDouble(firstInterpreter.Run("let x = 99\nx + 1"));

        using var secondProvider = BuildProvider();
        var secondInterpreter = GetCore(secondProvider, isCompiler: false);
        var fromSecondProvider = ToDouble(secondInterpreter.Run("let x = 3\nx + 1"));

        Assert.That(fromFirstProvider, Is.EqualTo(100).Within(1e-9));
        Assert.That(fromSecondProvider, Is.EqualTo(4).Within(1e-9));
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
