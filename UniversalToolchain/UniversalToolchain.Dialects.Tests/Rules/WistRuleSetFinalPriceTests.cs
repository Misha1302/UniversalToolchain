using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Tests.Rules;

public sealed class WistRuleSetFinalPriceTests
{
    private const string FinalPriceRules = """
                                          rule FinalPrice(price: number, quantity: number, discount: number, maxDiscount: number) -> number {
                                              let base = price * quantity
                                              let discountValue = clamp(base * discount, 0.0, maxDiscount)
                                              let result = base - discountValue

                                              if result < 0.0 then 0.0 else result
                                          }
                                          """;

    [TestCase("interpreter")]
    [TestCase("compiler")]
    public void CompileRuleSet_FinalPrice_ShouldRunEndToEnd(string backend)
    {
        using var facade = CreatePricingRulesFacade();

        var compileResult = facade.CompileRuleSet(FinalPriceRules, backend);

        Assert.That(compileResult.IsSuccess, Is.True, FormatDiagnostics(compileResult.Diagnostics));
        var runResult = compileResult.RuleSet.NotNull().TryRun(
            "FinalPrice",
            new Dictionary<string, object?>
            {
                ["price"] = 100.0,
                ["quantity"] = 3.0,
                ["discount"] = 0.15,
                ["maxDiscount"] = 50.0
            });

        Assert.Multiple(() =>
        {
            Assert.That(runResult.IsSuccess, Is.True, FormatDiagnostics(runResult.Diagnostics));
            Assert.That(ToDouble(runResult.Value), Is.EqualTo(255.0).Within(1e-9));
        });
    }

    private static WistRuntimeFacade CreatePricingRulesFacade()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeFile(ResolvePricingRulesDialectFile());
        Assert.That(
            composition.IsSuccess,
            Is.True,
            DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        return new WistRuntimeFacade(workflow.CreateHost(composition), composition);
    }

    private static string ResolvePricingRulesDialectFile()
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "..",
            "Dialects",
            "examples",
            "wist",
            "pricing-rules",
            "dialect.wistdialect"));
    }

    private static string FormatDiagnostics(IReadOnlyList<ToolchainDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(static x => $"{x.Code}: {x.Message}"));
    }

    private static double ToDouble(object? value)
    {
        return value switch
        {
            RealNumberImpl number => number.GetValue(),
            double doubleValue => doubleValue,
            float floatValue => floatValue,
            decimal decimalValue => (double)decimalValue,
            int intValue => intValue,
            long longValue => longValue,
            _ => Convert.ToDouble(value, CultureInfo.InvariantCulture)
        };
    }
}
