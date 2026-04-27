using UniversalToolchain.Capabilities.Abstractions;

namespace EqualityModule;

public sealed class EqualityCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId FeatureId = new("Assignments");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "Assignments",
                LanguageFeatureKind.Syntax,
                ["Equality"],
                [],
                [
                    new("=", LanguageFeatureSymbolKind.Operator, "target = value", "Assigns a value to a settable target.")
                ],
                ["cil", "interpreter"],
                "Provides assignment syntax for settable values.")
        ];
    }
}
