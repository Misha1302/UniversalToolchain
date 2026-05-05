using UniversalToolchain.Capabilities.Abstractions;

namespace VariablesModule;

public sealed class VariablesCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId _featureId = new("Variables");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
    [
        new(
            _featureId,
            "Variables",
            LanguageFeatureKind.Syntax,
            ["Variables"],
            [],
            [
                new LanguageFeatureSymbolDescriptor("let", LanguageFeatureSymbolKind.SyntaxForm, "let name[: type] = value", "Declares a local variable."),
                new LanguageFeatureSymbolDescriptor(":", LanguageFeatureSymbolKind.SyntaxForm, "name : type", "Separates a variable name from its declared type.")
            ],
            ["cil", "interpreter"],
            "Provides variable declaration syntax.")
    ];
}