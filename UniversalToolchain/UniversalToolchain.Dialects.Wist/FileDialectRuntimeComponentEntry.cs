namespace UniversalToolchain.Dialects.Wist;

public sealed record FileDialectRuntimeComponentEntry(
    string Kind,
    string CanonicalAlias,
    IReadOnlyList<string> Aliases,
    string TypeFullName);
