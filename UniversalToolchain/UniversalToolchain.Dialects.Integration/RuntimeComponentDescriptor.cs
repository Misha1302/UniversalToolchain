namespace UniversalToolchain.Dialects.Integration;

public sealed record RuntimeComponentDescriptor(
    RuntimeComponentId Id,
    RuntimeComponentKind Kind,
    string CanonicalAlias,
    IReadOnlyList<string> Aliases,
    Type ActivationType);
