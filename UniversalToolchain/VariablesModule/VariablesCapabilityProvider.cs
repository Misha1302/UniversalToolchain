using UniversalToolchain.Capabilities.Abstractions;

namespace VariablesModule;

public sealed class VariablesCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("Variables");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "Variables",
                LanguageFeatureKind.Syntax,
                ["Variables"],
                [],
                [
                    new("let", LanguageFeatureSymbolKind.SyntaxForm, "let name[: type] = value", "Declares a local variable."),
                    new(":", LanguageFeatureSymbolKind.SyntaxForm, "name : type", "Separates a variable name from its declared type.")
                ],
                ["cil", "interpreter"],
                "Provides variable declaration syntax.")
        ];
    }
}
