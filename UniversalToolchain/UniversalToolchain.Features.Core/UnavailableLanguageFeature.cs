using UniversalToolchain.Features.Abstractions;

namespace UniversalToolchain.Features.Core;

public sealed record UnavailableLanguageFeature(
    LanguageFeatureDescriptor Descriptor,
    IReadOnlyList<string> Reasons);
