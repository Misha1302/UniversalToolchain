namespace UniversalToolchain.Features.Abstractions;

/// <summary>
///     Describes one user-facing symbol exposed by a feature.
/// </summary>
public sealed record LanguageFeatureSymbolDescriptor(
    string Name,
    LanguageFeatureSymbolKind Kind,
    string Signature,
    string Description);
