using UniversalToolchain.Capabilities.Abstractions;

namespace LoopsModule;

public sealed class LoopsCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("LoopStatements");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
    [
        new(
            FeatureId,
            "Loop statements",
            LanguageFeatureKind.Syntax,
            ["Loops"],
            [],
            [
                new LanguageFeatureSymbolDescriptor("while", LanguageFeatureSymbolKind.SyntaxForm, "while <condition> <body>", "Repeats a block while a condition remains true."),
                new LanguageFeatureSymbolDescriptor("for", LanguageFeatureSymbolKind.SyntaxForm, "for <header> <body>", "Repeats a block according to a for-loop header.")
            ],
            ["cil", "interpreter"],
            "Provides loop constructs.")
    ];
}