using UniversalToolchain.Rules.Abstractions;
using UniversalToolchain.Rules.Core;

namespace UniversalToolchain.Dialects.Tests.Rules;

[TestFixture]
public sealed class RuleDiagnosticFormatterTests
{
    [Test]
    public void Formatter_SingleDiagnostic_ContainsCodeAndMessage()
    {
        var text = RuleDiagnosticFormatter.FormatDeterministic(
            [
                new RuleDiagnostic(
                    RuleDiagnosticCodes.UnknownFunction,
                    RuleDiagnosticSeverity.Error,
                    "Unknown function 'round'.",
                    null,
                    [])
            ]);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain(RuleDiagnosticCodes.UnknownFunction));
            Assert.That(text, Does.Contain("Unknown function 'round'."));
        });
    }

    [Test]
    public void Formatter_WithSpan_ContainsLineAndColumn()
    {
        var text = RuleDiagnosticFormatter.FormatDeterministic(
            [
                new RuleDiagnostic(
                    RuleDiagnosticCodes.TypeMismatch,
                    RuleDiagnosticSeverity.Error,
                    "Type mismatch.",
                    new SourceSpan("pricing.wist", 3, 7, 3, 12),
                    [])
            ]);

        Assert.That(text, Does.Contain("pricing.wist(3,7)"));
    }

    [Test]
    public void Formatter_RepeatedCalls_AreDeterministic()
    {
        var diagnostics =
            new[]
            {
                new RuleDiagnostic(
                    RuleDiagnosticCodes.TypeMismatch,
                    RuleDiagnosticSeverity.Error,
                    "Type mismatch.",
                    new SourceSpan("b.wist", 10, 4, 10, 6),
                    [new RuleDiagnosticHint("Check the binding type.")]),
                new RuleDiagnostic(
                    RuleDiagnosticCodes.UnknownFunction,
                    RuleDiagnosticSeverity.Error,
                    "Unknown function 'round'.",
                    new SourceSpan("a.wist", 1, 2, 1, 7),
                    [])
            };

        var first = RuleDiagnosticFormatter.FormatDeterministic(diagnostics);
        var second = RuleDiagnosticFormatter.FormatDeterministic(diagnostics);

        Assert.That(second, Is.EqualTo(first));
    }
}
