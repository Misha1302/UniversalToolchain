using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Dialects.Wist.Facade;

namespace UniversalToolchain.Dialects.Tests.Rules;

public sealed class WistRuleSetValidationTests
{
    [Test]
    public void CompileRuleSet_LetBindings_CanReferenceEarlierLocal()
    {
        const string source = """
                              rule Total(price: number) -> number {
                                  let base = price
                                  let doubled = base * 2.0
                                  doubled
                              }
                              """;

        using var facade = CreatePricingRulesFacade();
        var compile = facade.CompileRuleSet(source, "compiler");

        Assert.That(compile.IsSuccess, Is.True, FormatDiagnostics(compile.Diagnostics));
        var run = compile.RuleSet!.TryRun("Total", new Dictionary<string, object?> { ["price"] = 4.0 });
        Assert.That(run.IsSuccess, Is.True, FormatDiagnostics(run.Diagnostics));
    }

    [Test]
    public void CompileRuleSet_Locals_AreScopedPerRule()
    {
        const string source = """
                              rule A(price: number) -> number {
                                  let value = price * 2.0
                                  value
                              }

                              rule B(price: number) -> number {
                                  let value = price * 3.0
                                  value
                              }
                              """;

        using var facade = CreatePricingRulesFacade();
        var compile = facade.CompileRuleSet(source, "compiler");
        Assert.That(compile.IsSuccess, Is.True, FormatDiagnostics(compile.Diagnostics));

        var runA = compile.RuleSet!.TryRun("A", new Dictionary<string, object?> { ["price"] = 10.0 });
        var runB = compile.RuleSet.TryRun("B", new Dictionary<string, object?> { ["price"] = 10.0 });

        Assert.Multiple(() =>
        {
            Assert.That(runA.IsSuccess, Is.True, FormatDiagnostics(runA.Diagnostics));
            Assert.That(runB.IsSuccess, Is.True, FormatDiagnostics(runB.Diagnostics));
        });
    }

    [Test]
    public void TryRun_WhenArgumentsAreInvalid_ReturnsStructuredDiagnostics()
    {
        const string source = """
                              rule Total(price: number, quantity: number) -> number {
                                  price * quantity
                              }
                              """;

        using var facade = CreatePricingRulesFacade();
        var compile = facade.CompileRuleSet(source, "compiler");
        Assert.That(compile.IsSuccess, Is.True, FormatDiagnostics(compile.Diagnostics));

        var extra = compile.RuleSet!.TryRun("Total", new Dictionary<string, object?> { ["price"] = 1.0, ["quantity"] = 2.0, ["unexpected"] = 3.0 });
        var missing = compile.RuleSet.TryRun("Total", new Dictionary<string, object?> { ["price"] = 1.0 });
        var wrongType = compile.RuleSet.TryRun("Total", new Dictionary<string, object?> { ["price"] = "x", ["quantity"] = 2.0 });
        var nullValue = compile.RuleSet.TryRun("Total", new Dictionary<string, object?> { ["price"] = null, ["quantity"] = 2.0 });

        Assert.Multiple(() =>
        {
            Assert.That(extra.Diagnostics.Select(static x => x.Code), Does.Contain(ToolchainDiagnosticCodes.RuleArgumentUnknown));
            Assert.That(missing.Diagnostics.Select(static x => x.Code), Does.Contain(ToolchainDiagnosticCodes.RuleArgumentMissing));
            Assert.That(wrongType.Diagnostics.Select(static x => x.Code), Does.Contain(ToolchainDiagnosticCodes.RuleArgumentTypeMismatch));
            Assert.That(nullValue.Diagnostics.Select(static x => x.Code), Does.Contain(ToolchainDiagnosticCodes.RuleArgumentNull));
        });
    }

    private static WistRuntimeFacade CreatePricingRulesFacade()
    {
        return WistRuntimeFacadeBuilder
            .CreateDefault()
            .WithDialectFile(ResolvePricingRulesDialectFile())
            .Build();
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
}
