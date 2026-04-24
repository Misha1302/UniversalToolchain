using UniversalToolchain.Features.Abstractions;
using UniversalToolchain.Features.Core;

namespace UniversalToolchain.Dialects.Tests.Features;

[TestFixture]
public sealed class DialectFeatureExplanationFormatterTests
{
    [Test]
    public void Format_RepeatedCalls_ReturnSameText()
    {
        var explanation = new DialectFeatureExplanation(
            "PricingRestricted",
            [
                Available("NativeNumbers", "number"),
                Available("Scopes", "{ statements }"),
                Available("Variables", "let name = expression")
            ],
            [
                new UnavailableLanguageFeature(
                    Descriptor("CSharpInterop", ["CSharpInterop"], [], [], ["interpreter", "cil"]),
                    ["Required runtime component alias 'CSharpInterop' is not selected."])
            ],
            [
                new LanguageFeatureSymbolDescriptor("number", LanguageFeatureSymbolKind.Type, "number", "Native number."),
                new LanguageFeatureSymbolDescriptor("let", LanguageFeatureSymbolKind.SyntaxForm, "let name = expression", "Variable declaration.")
            ],
            [
                new DialectFeatureBackendSupport("cil", [new LanguageFeatureId("NativeNumbers"), new LanguageFeatureId("Scopes"), new LanguageFeatureId("Variables")]),
                new DialectFeatureBackendSupport("interpreter", [new LanguageFeatureId("NativeNumbers"), new LanguageFeatureId("Scopes"), new LanguageFeatureId("Variables")])
            ]);

        var first = DialectFeatureExplanationFormatter.FormatDeterministic(explanation);
        var second = DialectFeatureExplanationFormatter.FormatDeterministic(explanation);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Does.Contain("Dialect: PricingRestricted"));
            Assert.That(first, Does.Contain("- CSharpInterop: Required runtime component alias 'CSharpInterop' is not selected."));
            Assert.That(first, Does.Contain("- cil: NativeNumbers, Scopes, Variables"));
            Assert.That(first, Does.Contain("- type number"));
        });
    }

    private static AvailableLanguageFeature Available(string featureId, string symbolSignature)
    {
        return new AvailableLanguageFeature(
            Descriptor(
                featureId,
                ["Module" + featureId],
                [],
                [new LanguageFeatureSymbolDescriptor(featureId, LanguageFeatureSymbolKind.SyntaxForm, symbolSignature, featureId + " symbol.")],
                ["interpreter", "cil"]));
    }

    private static LanguageFeatureDescriptor Descriptor(
        string featureId,
        IReadOnlyList<string> requiredRuntimeAliases,
        IReadOnlyList<LanguageFeatureId> requiredFeatures,
        IReadOnlyList<LanguageFeatureSymbolDescriptor> symbols,
        IReadOnlyList<string> supportedBackends)
    {
        return new LanguageFeatureDescriptor(
            new LanguageFeatureId(featureId),
            featureId,
            LanguageFeatureKind.Syntax,
            requiredRuntimeAliases,
            requiredFeatures,
            symbols,
            supportedBackends,
            featureId + " description.");
    }
}
