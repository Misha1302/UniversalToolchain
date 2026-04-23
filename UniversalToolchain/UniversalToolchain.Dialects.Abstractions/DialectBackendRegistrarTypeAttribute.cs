using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
/// Declares the runtime registrar type that activates an exported backend declaration.
/// </summary>
/// <remarks>
/// The metadata is optional for manifest compatibility, but canonical exact backend loading can require it.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DialectBackendRegistrarTypeAttribute : Attribute
{
    public DialectBackendRegistrarTypeAttribute(Type registrarType)
    {
        RegistrarType = registrarType.ArgNotNull();
    }

    public Type RegistrarType { get; }
}
