using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Dialects.Wist.Rules;

namespace UniversalToolchain.Dialects.Tests.Rules;

public sealed class WistRuleDeclarationExtractorTests
{
    [Test]
    public void Extract_WhenRuleIsValid_ReturnsRuleModel()
    {
        var extractor = new WistRuleDeclarationExtractor();

        var result = extractor.Extract("""
                                     rule Total(price: number, enabled: bool) -> number {
                                         price
                                     }
                                     """);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Rules, Has.Count.EqualTo(1));
            Assert.That(result.Rules[0].Name, Is.EqualTo("Total"));
            Assert.That(result.Rules[0].Parameters.Select(static x => x.Name), Is.EqualTo(new[] { "price", "enabled" }));
            Assert.That(result.Rules[0].ReturnType.Name, Is.EqualTo("number"));
        });
    }

    [Test]
    public void Extract_WhenRuleNameIsDuplicated_ReturnsStructuredDiagnostic()
    {
        var extractor = new WistRuleDeclarationExtractor();

        var result = extractor.Extract("""
                                     rule Total(price: number) -> number { price }
                                     rule Total(price: number) -> number { price }
                                     """);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain(ToolchainDiagnosticCodes.RuleDuplicateName));
        });
    }

    [Test]
    public void Extract_WhenParameterNameIsDuplicated_ReturnsStructuredDiagnostic()
    {
        var extractor = new WistRuleDeclarationExtractor();

        var result = extractor.Extract("""
                                     rule Total(price: number, price: number) -> number { price }
                                     """);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain(ToolchainDiagnosticCodes.RuleDuplicateParameter));
        });
    }

    [Test]
    public void Extract_WhenTypeIsUnknown_ReturnsStructuredDiagnostic()
    {
        var extractor = new WistRuleDeclarationExtractor();

        var result = extractor.Extract("""
                                     rule Total(price: money) -> number { price }
                                     """);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain(ToolchainDiagnosticCodes.RuleUnknownType));
        });
    }
}
