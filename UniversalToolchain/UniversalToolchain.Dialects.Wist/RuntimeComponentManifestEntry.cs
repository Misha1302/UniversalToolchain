namespace UniversalToolchain.Dialects.Wist;

public sealed record RuntimeComponentManifestEntry(
    RuntimeComponentKind Kind,
    string CanonicalAlias,
    IReadOnlyList<string> Aliases,
    string AssemblySimpleName,
    string TypeFullName)
{
    public IReadOnlyList<string> AllAliases => [CanonicalAlias, .. Aliases];
}
