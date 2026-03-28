using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectMinimalRuntimeParityTests
{
    [Test]
    public void MinimalPath_MinimalArithmetic_ProducesExpectedExecutionResult() => AssertResultForExample("minimal-arithmetic", "interpreter", expected: 14d);

    [Test]
    public void MinimalPath_WithOptimizer_ProducesStableExecutionResult()
    {
        var examplePath = ResolveExampleDirectory("minimal-arithmetic");
        var dialectText = File.ReadAllText(Path.Combine(examplePath, "dialect.wistdialect"));
        var code = File.ReadAllText(Path.Combine(examplePath, "program.wist"));

        var first = Execute(CreateMinimalProvider(), dialectText, code, "interpreter");
        var second = Execute(CreateMinimalProvider(), dialectText, code, "interpreter");

        Assert.That(first, Is.EqualTo(second).Within(1e-9));
    }

    [Test]
    public void MinimalPath_InlineDialect_ComposesAndRunsInterpreter()
        => AssertResultForInlineDialect("dialect Demo\nuse Arithmetic,Numbers,Scopes\nbackend interpreter", "2+5", "interpreter");

    private static void AssertResultForExample(string exampleName, string executionMode, double expected)
    {
        var examplePath = ResolveExampleDirectory(exampleName);
        var dialectText = File.ReadAllText(Path.Combine(examplePath, "dialect.wistdialect"));
        var code = File.ReadAllText(Path.Combine(examplePath, "program.wist"));
        AssertResultForInlineDialect(dialectText, code, executionMode, expected);
    }

    private static void AssertResultForInlineDialect(string dialectText, string code, string executionMode, double? expected = null)
    {
        var minimal = Execute(CreateMinimalProvider(), dialectText, code, executionMode);
        if (expected.HasValue)
            Assert.That(minimal, Is.EqualTo(expected.Value).Within(1e-9));
        else
            Assert.That(double.IsFinite(minimal), Is.True);
    }

    private static double Execute(ServiceProvider provider, string dialectText, string code, string executionMode)
    {
        using (provider)
        {
            var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
            var composition = workflow.ComposeText(dialectText, "inline");
            using var host = workflow.CreateHost(composition);
            return ToDouble(host.Run(code, executionMode));
        }
    }

    private static ServiceProvider CreateMinimalProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServicesMinimal();
        return services.BuildServiceProvider();
    }

    private static string ResolveExampleDirectory(string name)
    {
        var path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", name));
        return path;
    }

    private static double ToDouble(object? value)
    {
        return value switch
        {
            RealNumberImpl number => number.GetValue(),
            int intValue => intValue,
            long longValue => longValue,
            double doubleValue => doubleValue,
            float floatValue => floatValue,
            decimal decimalValue => (double)decimalValue,
            _ => Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture)
        };
    }
}
