namespace UniversalToolchain.Capabilities.Abstractions;

public sealed record LanguageFeatureDescriptor(
    LanguageFeatureId FeatureId,
    string DisplayName,
    LanguageFeatureKind Kind,
    IReadOnlyList<string> RequiredRuntimeComponentAliases,
    IReadOnlyList<LanguageFeatureId> RequiredFeatures,
    IReadOnlyList<LanguageFeatureSymbolDescriptor> ProvidedSymbols,
    IReadOnlyList<string> SupportedBackendAliases,
    string ShortDescription);