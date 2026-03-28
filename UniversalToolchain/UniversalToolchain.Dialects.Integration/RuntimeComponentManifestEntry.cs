namespace UniversalToolchain.Dialects.Integration;

public sealed record RuntimeComponentManifestEntry(
    RuntimeComponentKind Kind,
    string CanonicalAlias,
    IReadOnlyList<string> Aliases,
    RuntimeTypeReference TypeReference)
{
    public IReadOnlyList<string> AllAliases => [CanonicalAlias, .. Aliases];
}