using System.Globalization;
using NumbersModule.Core;
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
        Assert.Multiple(() =>
        {
            Assert.That(run.IsSuccess, Is.True, FormatDiagnostics(run.Diagnostics));
            Assert.That(ToDouble(run.Value), Is.EqualTo(8.0).Within(1e-9));
        });
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
            Assert.That(ToDouble(runA.Value), Is.EqualTo(20.0).Within(1e-9));
            Assert.That(ToDouble(runB.Value), Is.EqualTo(30.0).Within(1e-9));
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
            Assert.That(extra.IsSuccess, Is.False);
            Assert.That(missing.IsSuccess, Is.False);
            Assert.That(wrongType.IsSuccess, Is.False);
            Assert.That(nullValue.IsSuccess, Is.False);
            Assert.That(extra.Diagnostics.Select(static x => x.Code), Does.Contain(ToolchainDiagnosticCodes.RuleArgumentUnknown));
            Assert.That(missing.Diagnostics.Select(static x => x.Code), Does.Contain(ToolchainDiagnosticCodes.RuleArgumentMissing));
            Assert.That(wrongType.Diagnostics.Select(static x => x.Code), Does.Contain(ToolchainDiagnosticCodes.RuleArgumentTypeMismatch));
            Assert.That(nullValue.Diagnostics.Select(static x => x.Code), Does.Contain(ToolchainDiagnosticCodes.RuleArgumentNull));
        });
    }

    [Test]
    public void TryRun_WhenExtraArgumentMatchesLocalName_ReturnsUnknownArgumentDiagnostic()
    {
        const string source = """
                              rule Total(price: number) -> number {
                                  let value = price * 2.0
                                  value
                              }
                              """;

        using var facade = CreatePricingRulesFacade();
        var compile = facade.CompileRuleSet(source, "compiler");
        Assert.That(compile.IsSuccess, Is.True, FormatDiagnostics(compile.Diagnostics));

        var run = compile.RuleSet!.TryRun(
            "Total",
            new Dictionary<string, object?>
            {
                ["price"] = 10.0,
                ["value"] = 999.0
            });

        Assert.Multiple(() =>
        {
            Assert.That(run.IsSuccess, Is.False);
            Assert.That(run.Diagnostics.Select(static x => x.Code), Does.Contain(ToolchainDiagnosticCodes.RuleArgumentUnknown));
        });
    }

    [Test]
    public void CompileRuleSet_WhenDuplicateLocalLetBindingExists_ReturnsDiagnostic()
    {
        const string source = """
                              rule Bad(price: number) -> number {
                                  let value = price
                                  let value = price * 2.0
                                  value
                              }
                              """;

        using var facade = CreatePricingRulesFacade();
        var compile = facade.CompileRuleSet(source, "compiler");

        Assert.Multiple(() =>
        {
            Assert.That(compile.IsSuccess, Is.False);
            Assert.That(compile.Diagnostics.Select(static x => x.Code), Does.Contain(ToolchainDiagnosticCodes.RuleDuplicateLocal));
        });
    }

    [Test]
    public void CompileRuleSet_WhenLocalLetBindingShadowsParameter_ReturnsDiagnostic()
    {
        const string source = """
                              rule Bad(price: number) -> number {
                                  let price = 10.0
                                  price
                              }
                              """;

        using var facade = CreatePricingRulesFacade();
        var compile = facade.CompileRuleSet(source, "compiler");

        Assert.Multiple(() =>
        {
            Assert.That(compile.IsSuccess, Is.False);
            Assert.That(compile.Diagnostics.Select(static x => x.Code), Does.Contain(ToolchainDiagnosticCodes.RuleLocalShadowsParameter));
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
