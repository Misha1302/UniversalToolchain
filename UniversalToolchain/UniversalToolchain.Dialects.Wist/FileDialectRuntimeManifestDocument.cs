namespace UniversalToolchain.Dialects.Wist;

public sealed record FileDialectRuntimeManifestDocument(
    string DialectFamily,
    string AssemblySimpleName,
    IReadOnlyList<FileDialectRuntimeComponentEntry> Components);
