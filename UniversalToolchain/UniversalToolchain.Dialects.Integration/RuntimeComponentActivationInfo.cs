namespace UniversalToolchain.Dialects.Integration;

public sealed record RuntimeComponentActivationInfo(
    RuntimeTypeReference ActivationType,
    RuntimeTypeReference? RegistrarType = null)
{
    public string ActivationTypeFullName => ActivationType.TypeFullName;

    public string ActivationAssemblySimpleName => ActivationType.AssemblySimpleName;

    public string? RegistrarTypeFullName => RegistrarType?.TypeFullName;

    public string? RegistrarAssemblySimpleName => RegistrarType?.AssemblySimpleName;
}
