using UniversalToolchain.Capabilities.Abstractions;

namespace ConditionsModule;

public sealed class ConditionalBranchesCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("ConditionalBranches");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
    [
        new(
            FeatureId,
            "Conditional branches",
            LanguageFeatureKind.Syntax,
            ["Conditions"],
            [],
            [
                new LanguageFeatureSymbolDescriptor("if", LanguageFeatureSymbolKind.SyntaxForm, "if <condition> <body>", "Starts a conditional branch."),
                new LanguageFeatureSymbolDescriptor("elif", LanguageFeatureSymbolKind.SyntaxForm, "elif <condition> <body>", "Adds a follow-up conditional branch."),
                new LanguageFeatureSymbolDescriptor("else", LanguageFeatureSymbolKind.SyntaxForm, "else <body>", "Adds a fallback branch.")
            ],
            ["cil", "interpreter"],
            "Provides statement-style conditional branch constructs.")
    ];
}