namespace UniversalToolchain.Capabilities.Abstractions;

public sealed record LanguageFeatureSymbolDescriptor(
    string Name,
    LanguageFeatureSymbolKind Kind,
    string Signature,
    string Description);