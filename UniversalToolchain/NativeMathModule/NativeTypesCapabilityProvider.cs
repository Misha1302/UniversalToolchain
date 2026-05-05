using UniversalToolchain.Capabilities.Abstractions;

namespace NativeMathModule;

public sealed class NativeTypesCapabilityProvider : ILanguageFeatureDescriptorProvider
{
    private static readonly LanguageFeatureId _featureId = new("NativeNumericTypes");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures() =>
    [
        new(
            _featureId,
            "Native numeric types",
            LanguageFeatureKind.TypeSystem,
            ["NativeTypes"],
            [],
            [
                new LanguageFeatureSymbolDescriptor("int", LanguageFeatureSymbolKind.Type, "int", "Provides native integer values without a literal suffix."),
                new LanguageFeatureSymbolDescriptor("long", LanguageFeatureSymbolKind.Type, "long", "Provides long integer values through the 'l' suffix."),
                new LanguageFeatureSymbolDescriptor("float", LanguageFeatureSymbolKind.Type, "float", "Provides single-precision values through the 'f' suffix."),
                new LanguageFeatureSymbolDescriptor("double", LanguageFeatureSymbolKind.Type, "double", "Provides double-precision values through the 'd' suffix or floating-point inference."),
                new LanguageFeatureSymbolDescriptor("decimal", LanguageFeatureSymbolKind.Type, "decimal", "Provides decimal values through the 'm' suffix.")
            ],
            ["cil", "interpreter"],
            "Provides native numeric literal parsing and arithmetic node shapes.")
    ];
}