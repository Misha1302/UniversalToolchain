namespace UniversalToolchain.Dialects.Integration;

public sealed record FileDialectRuntimeManifestDocument(
    string AssemblySimpleName,
    IReadOnlyList<FileDialectRuntimeComponentEntry> Components);