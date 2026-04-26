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
    public void CompileRuleSet_WhenCommentContainsLet_DoesNotCreateFakeLocal()
    {
        const string source = """
                              rule Total(price: number) -> number {
                                  // let value = 100.0
                                  let real = price
                                  real
                              }
                              """;

        using var facade = CreatePricingRulesFacade();
        var compile = facade.CompileRuleSet(source, "compiler");

        Assert.That(compile.IsSuccess, Is.True, FormatDiagnostics(compile.Diagnostics));
    }


    [Test]
    public void CompileRuleSet_LocalBindingValidation_IsNotPerformedUntilAstBackedExtractionExists()
    {
        const string source = """
                              rule SameNameLocals(price: number) -> number {
                                  let value = price
                                  let value = price * 2.0
                                  value
                              }

                              rule LocalShadowsParameter(price: number) -> number {
                                  let price = 10.0
                                  price
                              }
                              """;

        using var facade = CreatePricingRulesFacade();
        var compile = facade.CompileRuleSet(source, "compiler");

        Assert.Multiple(() =>
        {
            Assert.That(compile.IsSuccess, Is.True, FormatDiagnostics(compile.Diagnostics));
            Assert.That(compile.Diagnostics.Select(static x => x.Code), Does.Not.Contain(ToolchainDiagnosticCodes.RuleDuplicateLocal));
            Assert.That(compile.Diagnostics.Select(static x => x.Code), Does.Not.Contain(ToolchainDiagnosticCodes.RuleLocalShadowsParameter));
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
    public void CompileRuleSet_InterpreterAndCompiler_ShouldHaveParity()
    {
        const string source = """
                              rule Total(price: number) -> number {
                                  let value = price * 2.0
                                  value
                              }
                              """;

        using var facade = CreatePricingRulesFacade();
        var compilerCompile = facade.CompileRuleSet(source, "compiler");
        var interpreterCompile = facade.CompileRuleSet(source, "interpreter");

        Assert.That(compilerCompile.IsSuccess, Is.True, FormatDiagnostics(compilerCompile.Diagnostics));
        Assert.That(interpreterCompile.IsSuccess, Is.True, FormatDiagnostics(interpreterCompile.Diagnostics));

        var args = new Dictionary<string, object?> { ["price"] = 10.0 };
        var compilerRun = compilerCompile.RuleSet!.TryRun("Total", args);
        var interpreterRun = interpreterCompile.RuleSet!.TryRun("Total", args);

        Assert.Multiple(() =>
        {
            Assert.That(compilerRun.IsSuccess, Is.True, FormatDiagnostics(compilerRun.Diagnostics));
            Assert.That(interpreterRun.IsSuccess, Is.True, FormatDiagnostics(interpreterRun.Diagnostics));
            Assert.That(ToDouble(compilerRun.Value), Is.EqualTo(ToDouble(interpreterRun.Value)).Within(1e-9));
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
