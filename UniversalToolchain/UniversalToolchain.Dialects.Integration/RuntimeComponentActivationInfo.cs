namespace UniversalToolchain.Dialects.Integration;

public sealed record RuntimeComponentActivationInfo(
    string ActivationTypeFullName,
    string? RegistrarTypeFullName = null);
