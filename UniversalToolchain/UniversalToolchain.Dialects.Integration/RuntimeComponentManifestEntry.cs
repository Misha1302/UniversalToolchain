namespace UniversalToolchain.Dialects.Integration;

public sealed record RuntimeComponentManifestEntry(
    RuntimeComponentKind Kind,
    string CanonicalAlias,
    IReadOnlyList<string> Aliases,
    RuntimeComponentId ComponentId,
    string AssemblySimpleName)
{
    public IReadOnlyList<string> AllAliases => [CanonicalAlias, .. Aliases];
}
