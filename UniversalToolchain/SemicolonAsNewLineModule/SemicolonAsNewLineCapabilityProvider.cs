using UniversalToolchain.Capabilities.Abstractions;

namespace SemicolonAsNewLineModule;

public sealed class SemicolonAsNewLineCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("SemicolonAsNewLine");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "Semicolon statement separators",
                LanguageFeatureKind.Syntax,
                ["SemicolonAsNewLine"],
                [],
                [
                    new(";", LanguageFeatureSymbolKind.SyntaxForm, "statement ; statement", "Treats a semicolon as a newline-equivalent statement separator.")
                ],
                ["cil", "interpreter"],
                "Provides semicolon-based statement separation.")
        ];
    }
}
