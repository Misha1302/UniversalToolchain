using UniversalToolchain.Features.Abstractions;

namespace UniversalToolchain.Features.Core;

public sealed record DialectFeatureBackendSupport(
    string BackendAlias,
    IReadOnlyList<LanguageFeatureId> SupportedFeatures);
