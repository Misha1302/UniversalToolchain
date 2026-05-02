using UniversalToolchain.Capabilities.Abstractions;

namespace LabelsModule;

public sealed class LabelsCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("LabelsAndGoto");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
    [
        new(
            FeatureId,
            "Labels and goto",
            LanguageFeatureKind.Syntax,
            ["Labels"],
            [],
            [
                new LanguageFeatureSymbolDescriptor("label", LanguageFeatureSymbolKind.SyntaxForm, "name:", "Defines a jump target label."),
                new LanguageFeatureSymbolDescriptor("goto", LanguageFeatureSymbolKind.SyntaxForm, "goto name", "Transfers control to a named label.")
            ],
            ["cil", "interpreter"],
            "Provides labels and goto-based control flow.")
    ];
}