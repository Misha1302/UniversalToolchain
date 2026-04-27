using UniversalToolchain.Capabilities.Abstractions;

namespace LoopsModule;

public sealed class LoopsCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("LoopStatements");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "Loop statements",
                LanguageFeatureKind.Syntax,
                ["Loops"],
                [],
                [
                    new("while", LanguageFeatureSymbolKind.SyntaxForm, "while <condition> <body>", "Repeats a block while a condition remains true."),
                    new("for", LanguageFeatureSymbolKind.SyntaxForm, "for <header> <body>", "Repeats a block according to a for-loop header.")
                ],
                ["cil", "interpreter"],
                "Provides loop constructs.")
        ];
    }
}
