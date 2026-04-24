namespace UniversalToolchain.Features.Abstractions;

/// <summary>
///     Describes a user-facing language capability independently from runtime activation.
/// </summary>
public sealed record LanguageFeatureDescriptor(
    LanguageFeatureId FeatureId,
    string DisplayName,
    LanguageFeatureKind Kind,
    IReadOnlyList<string> RequiredRuntimeComponentAliases,
    IReadOnlyList<LanguageFeatureId> RequiredFeatures,
    IReadOnlyList<LanguageFeatureSymbolDescriptor> ProvidedSymbols,
    IReadOnlyList<string> SupportedBackendAliases,
    string ShortDescription);
