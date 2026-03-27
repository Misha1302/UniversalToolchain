namespace UniversalToolchain.Dialects.Wist;

public sealed record RuntimeTypeReference(
    string AssemblySimpleName,
    string TypeFullName);
