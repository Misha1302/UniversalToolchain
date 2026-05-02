using UniversalToolchain.Capabilities.Abstractions;

namespace ScopesModule;

public sealed class ScopesCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("ParenthesizedScopes");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
    [
        new(
            FeatureId,
            "Parenthesized scopes",
            LanguageFeatureKind.Syntax,
            ["Scopes"],
            [],
            [
                new LanguageFeatureSymbolDescriptor("(", LanguageFeatureSymbolKind.SyntaxForm, "( expression )", "Starts a parenthesized scope."),
                new LanguageFeatureSymbolDescriptor(")", LanguageFeatureSymbolKind.SyntaxForm, "( expression )", "Ends a parenthesized scope.")
            ],
            ["cil", "interpreter"],
            "Provides parenthesized grouping for expressions.")
    ];
}