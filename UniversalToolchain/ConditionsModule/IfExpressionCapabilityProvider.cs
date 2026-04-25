using UniversalToolchain.Capabilities.Abstractions;

namespace ConditionsModule;

public sealed class IfExpressionCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("IfExpressions");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "Conditional expressions",
                LanguageFeatureKind.Syntax,
                ["Conditions"],
                [],
                [
                    new("if", LanguageFeatureSymbolKind.SyntaxForm, "if <condition> <body>", "Starts a conditional branch."),
                    new("elif", LanguageFeatureSymbolKind.SyntaxForm, "elif <condition> <body>", "Adds a follow-up conditional branch."),
                    new("else", LanguageFeatureSymbolKind.SyntaxForm, "else <body>", "Adds a fallback branch.")
                ],
                ["cil", "interpreter"],
                "Provides if, elif, and else conditional constructs.")
        ];
    }
}
