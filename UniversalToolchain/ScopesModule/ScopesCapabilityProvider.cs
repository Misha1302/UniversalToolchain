using UniversalToolchain.Capabilities.Abstractions;

namespace ScopesModule;

public sealed class ScopesCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("ParenthesizedScopes");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "Parenthesized scopes",
                LanguageFeatureKind.Syntax,
                ["Scopes"],
                [],
                [
                    new("(", LanguageFeatureSymbolKind.SyntaxForm, "( expression )", "Starts a parenthesized scope."),
                    new(")", LanguageFeatureSymbolKind.SyntaxForm, "( expression )", "Ends a parenthesized scope.")
                ],
                ["cil", "interpreter"],
                "Provides parenthesized grouping for expressions.")
        ];
    }
}
