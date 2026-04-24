using UniversalToolchain.Features.Abstractions;

namespace UniversalToolchain.Features.Core;

public sealed record DialectFeatureExplanation(
    string DialectName,
    IReadOnlyList<AvailableLanguageFeature> AvailableFeatures,
    IReadOnlyList<UnavailableLanguageFeature> UnavailableFeatures,
    IReadOnlyList<LanguageFeatureSymbolDescriptor> AvailableSymbols,
    IReadOnlyList<DialectFeatureBackendSupport> BackendSupport);
