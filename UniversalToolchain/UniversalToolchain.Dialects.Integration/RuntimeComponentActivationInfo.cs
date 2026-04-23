namespace UniversalToolchain.Dialects.Integration;

public sealed record RuntimeComponentActivationInfo(
    RuntimeTypeReference ActivationType,
    RuntimeTypeReference? RegistrarType = null)
{
    public RuntimeComponentActivationInfo(string activationTypeFullName, string? registrarTypeFullName = null)
        : this(
            new RuntimeTypeReference(RuntimeAssemblyIdentity.UnspecifiedAssemblySimpleName, activationTypeFullName),
            string.IsNullOrWhiteSpace(registrarTypeFullName)
                ? null
                : new RuntimeTypeReference(RuntimeAssemblyIdentity.UnspecifiedAssemblySimpleName, registrarTypeFullName))
    {
    }

    public string ActivationTypeFullName => ActivationType.TypeFullName;

    public string ActivationAssemblySimpleName => ActivationType.AssemblySimpleName;

    public string? RegistrarTypeFullName => RegistrarType?.TypeFullName;

    public string? RegistrarAssemblySimpleName => RegistrarType?.AssemblySimpleName;
}
