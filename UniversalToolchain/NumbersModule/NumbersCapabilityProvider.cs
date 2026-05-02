using UniversalToolchain.Capabilities.Abstractions;

namespace NumbersModule;

public sealed class NumbersCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("NumericLiterals");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
    [
        new(
            FeatureId,
            "Numeric literals",
            LanguageFeatureKind.Syntax,
            ["Numbers"],
            [],
            [
                new LanguageFeatureSymbolDescriptor("number", LanguageFeatureSymbolKind.SyntaxForm, "123 | 1.25 | 6.02e23", "Parses decimal and exponent-based numeric literals.")
            ],
            ["cil", "interpreter"],
            "Provides numeric literal parsing for Wist expressions.")
    ];
}