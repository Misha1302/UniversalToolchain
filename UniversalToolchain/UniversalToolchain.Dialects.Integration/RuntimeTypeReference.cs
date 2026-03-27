namespace UniversalToolchain.Dialects.Integration;

public sealed record RuntimeTypeReference(
    string AssemblySimpleName,
    string TypeFullName);
