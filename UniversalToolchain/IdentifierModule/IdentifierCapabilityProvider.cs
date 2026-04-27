using UniversalToolchain.Capabilities.Abstractions;

namespace IdentifierModule;

public sealed class IdentifierCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("Identifiers");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "Identifiers",
                LanguageFeatureKind.Syntax,
                ["Identifier"],
                [],
                [
                    new("identifier", LanguageFeatureSymbolKind.SyntaxForm, "name | namespace.member | Generic<Type>", "Parses identifiers, dotted names, and generic-looking identifiers.")
                ],
                ["cil", "interpreter"],
                "Provides identifier parsing for names and member-like references.")
        ];
    }
}
