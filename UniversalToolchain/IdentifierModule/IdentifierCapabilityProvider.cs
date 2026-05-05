using UniversalToolchain.Capabilities.Abstractions;

namespace IdentifierModule;

public sealed class IdentifierCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId _featureId = new("Identifiers");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
    [
        new(
            _featureId,
            "Identifiers",
            LanguageFeatureKind.Syntax,
            ["Identifier"],
            [],
            [
                new LanguageFeatureSymbolDescriptor("identifier", LanguageFeatureSymbolKind.SyntaxForm, "name | namespace.member | Generic<Type>", "Parses identifiers, dotted names, and generic-looking identifiers.")
            ],
            ["cil", "interpreter"],
            "Provides identifier parsing for names and member-like references.")
    ];
}