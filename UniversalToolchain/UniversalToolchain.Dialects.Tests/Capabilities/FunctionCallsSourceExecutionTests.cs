using System.Globalization;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests.Capabilities;

public sealed class FunctionCallsSourceExecutionTests
{
    [TestCase("min(10.0, 3.0)", "3")]
    [TestCase("max(10.0, 3.0)", "10")]
    [TestCase("abs(0.0 - 5.0)", "5")]
    [TestCase("clamp(120.0, 0.0, 100.0)", "100")]
    [TestCase("let x = 120.0\nclamp(x, 0.0, 100.0)", "100")]
    [TestCase("let base = 300.0\nlet discount = 0.15\nlet maxDiscount = 50.0\nclamp(base * discount, 0.0, maxDiscount)", "45")]
    [TestCase("round(2.6)", "3")]
    public void SafeMathFunctionCalls_FromWistSource_ShouldHaveInterpreterAndCompilerParity(
        string source,
        string expectedValueText)
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(ResolveFunctionCallsSafeMathDialectFile());
        Assert.That(
            composition.IsSuccess,
            Is.True,
            DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        using var host = workflow.CreateHost(composition);

        var interpreter = Normalize(host.Run(source, "interpreter"));
        var compiler = Normalize(host.Run(source, "compiler"));

        Assert.Multiple(() =>
        {
            Assert.That(interpreter, Is.EqualTo(expectedValueText));
            Assert.That(compiler, Is.EqualTo(expectedValueText));
            Assert.That(compiler, Is.EqualTo(interpreter));
        });
    }

    [Test]
    public void FinalPriceExpression_FromWistSource_ShouldHaveInterpreterAndCompilerParity()
    {
        const string source = """
                              let base = 100.0 * 3.0
                              let discountValue = clamp(base * 0.15, 0.0, 50.0)
                              let result = base - discountValue
                              if result < 0.0 then 0.0 else result
                              """;

        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(ResolveFunctionCallsSafeMathDialectFile());
        Assert.That(
            composition.IsSuccess,
            Is.True,
            DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        using var host = workflow.CreateHost(composition);
        var interpreter = Normalize(host.Run(source, "interpreter"));
        var compiler = Normalize(host.Run(source, "compiler"));

        Assert.Multiple(() =>
        {
            Assert.That(interpreter, Is.EqualTo("255"));
            Assert.That(compiler, Is.EqualTo("255"));
        });
    }

    private static string ResolveFunctionCallsSafeMathDialectFile() =>
        UniversalToolchain.Dialects.Tests.TestSourcePaths.WistExampleDialectPath("function-calls-safe-math");

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }

    private static string Normalize(object? value)
    {
        return value switch
        {
            RealNumberImpl number => number.GetValue().ToString(CultureInfo.InvariantCulture),
            double doubleValue => doubleValue.ToString("G17", CultureInfo.InvariantCulture),
            null => "<null>",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString() ?? "<unknown>"
        };
    }
}