namespace UniversalToolchain.Dialects.Integration;

public sealed record FileRuntimeComponentActivationEntry(
    string ActivationTypeFullName,
    string? RegistrarTypeFullName = null);
