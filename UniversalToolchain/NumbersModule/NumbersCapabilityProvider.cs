using UniversalToolchain.Capabilities.Abstractions;

namespace NumbersModule;

public sealed class NumbersCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("NumericLiterals");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "Numeric literals",
                LanguageFeatureKind.Syntax,
                ["Numbers"],
                [],
                [
                    new("number", LanguageFeatureSymbolKind.SyntaxForm, "123 | 1.25 | 6.02e23", "Parses decimal and exponent-based numeric literals.")
                ],
                ["cil", "interpreter"],
                "Provides numeric literal parsing for Wist expressions.")
        ];
    }
}
