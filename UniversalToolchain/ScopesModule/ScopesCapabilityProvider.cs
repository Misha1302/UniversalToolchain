using UniversalToolchain.Capabilities.Abstractions;

namespace ScopesModule;

public sealed class ScopesCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId _featureId = new("ParenthesizedScopes");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
    [
        new(
            _featureId,
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