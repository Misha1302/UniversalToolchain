using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectMinimalRuntimeParityTests
{
    [Test]
    public void MinimalPath_FullDefault_ProducesSameExecutionResult_AsLegacyPath() => AssertParityForExample("full-default", "interpreter", expected: 15d);

    [Test]
    public void MinimalPath_MinimalArithmetic_ProducesSameExecutionResult_AsLegacyPath() => AssertParityForExample("minimal-arithmetic", "interpreter", expected: 14d);


    [Test]
    public void MinimalPath_WithOptimizer_ProducesSameExecutionResult_AsLegacyPath()
    {
        var examplePath = ResolveExampleDirectory("full-default");
        var dialectText = File.ReadAllText(Path.Combine(examplePath, "dialect.wistdialect"));
        var code = File.ReadAllText(Path.Combine(examplePath, "program.wist"));

        var minimal = Execute(CreateMinimalProvider(), dialectText, code, "interpreter");
        var legacy = Execute(CreateLegacyProvider(), dialectText, code, "interpreter");

        Assert.That(minimal, Is.EqualTo(legacy).Within(1e-9));
    }

    [Test]
    public void MinimalPath_InterpreterOnly_ProducesSameExecutionResult_AsLegacyPath() => AssertParityForInlineDialect("dialect Demo\nuse Arithmetic,Numbers,Whitespaces\nbackend interpreter", "2 + 5", "interpreter");

    [Test]
    public void MinimalPath_CilOnly_ProducesSameExecutionResult_AsLegacyPath() => AssertParityForInlineDialect("dialect Demo\nuse Arithmetic,Numbers,Whitespaces\nbackend compiler", "2 + 5", "compiler");

    private static void AssertParityForExample(string exampleName, string executionMode, double expected)
    {
        var examplePath = ResolveExampleDirectory(exampleName);
        var dialectText = File.ReadAllText(Path.Combine(examplePath, "dialect.wistdialect"));
        var code = File.ReadAllText(Path.Combine(examplePath, "program.wist"));
        AssertParityForInlineDialect(dialectText, code, executionMode, expected);
    }

    private static void AssertParityForInlineDialect(string dialectText, string code, string executionMode, double? expected = null)
    {
        var minimal = Execute(CreateMinimalProvider(), dialectText, code, executionMode);
        var legacy = Execute(CreateLegacyProvider(), dialectText, code, executionMode);

        Assert.That(minimal, Is.EqualTo(legacy).Within(1e-9));
        if (expected.HasValue)
            Assert.That(minimal, Is.EqualTo(expected.Value).Within(1e-9));
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
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateLegacyProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
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
