using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectMinimalRuntimeSmokeTests
{
    [Test]
    public void MinimalPath_FullDefault_ComposesAndRunsSuccessfully() => AssertForExample("full-default", "interpreter", 15d);

    [Test]
    public void MinimalPath_MinimalArithmetic_ComposesAndRunsSuccessfully() => AssertForExample("minimal-arithmetic", "interpreter", 14d);

    [Test]
    public void MinimalPath_WithOptimizer_ComposesAndRunsSuccessfully() => AssertForExample("full-default", "interpreter", 15d);

    [Test]
    public void MinimalPath_InterpreterOnly_ComposesAndRunsSuccessfully() => AssertForInlineDialect("dialect Demo\nuse Arithmetic,Numbers,Whitespaces\nbackend interpreter", "2 + 5", "interpreter");

    [Test]
    public void MinimalPath_CilOnly_ComposesAndRunsSuccessfully() => AssertForInlineDialect("dialect Demo\nuse Arithmetic,Numbers,Whitespaces\nbackend compiler", "2 + 5", "compiler");

    private static void AssertForExample(string exampleName, string executionMode, double expected)
    {
        var examplePath = ResolveExampleDirectory(exampleName);
        var dialectText = File.ReadAllText(Path.Combine(examplePath, "dialect.wistdialect"));
        var code = File.ReadAllText(Path.Combine(examplePath, "program.wist"));
        AssertForInlineDialect(dialectText, code, executionMode, expected);
    }

    private static void AssertForInlineDialect(string dialectText, string code, string executionMode, double? expected = null)
    {
        using var provider = CreateMinimalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(dialectText, "inline");

        Assert.That(composition.IsSuccess, Is.True, DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        using var host = workflow.CreateHost(composition);
        var output = host.Run(code, executionMode);

        if (expected.HasValue)
        {
            Assert.That(output, Is.Not.Null);
            var result = ToDouble(output);
            Assert.That(result, Is.EqualTo(expected.Value).Within(1e-9));
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
            _ => Convert.ToDouble(value, CultureInfo.InvariantCulture)
        };
    }
}