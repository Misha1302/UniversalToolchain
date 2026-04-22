namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Describes how an intrinsic descriptor provider is registered in the service collection.
/// </summary>
public enum IntrinsicDescriptorProviderRegistrationKind
{
    ImplementationType,
    ImplementationInstance,
    Factory
}
