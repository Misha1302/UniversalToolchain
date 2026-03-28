namespace UniversalToolchain.Dialects.Integration;

public sealed record FileDialectRuntimeComponentEntry(
    string Kind,
    string CanonicalAlias,
    IReadOnlyList<string> Aliases,
    string ComponentId);