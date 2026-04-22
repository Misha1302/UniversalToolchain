using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Captures declaration-time details for an intrinsic descriptor provider registration.
/// </summary>
public sealed class IntrinsicDescriptorProviderRegistration
{
    public IntrinsicDescriptorProviderRegistration(
        int registrationIndex,
        IntrinsicDescriptorProviderRegistrationKind kind,
        Type? providerType)
    {
        if (registrationIndex < 0)
        {
            Thrower.Argument(nameof(registrationIndex), "Registration index must not be negative.");
        }

        RegistrationIndex = registrationIndex;
        Kind = kind;
        ProviderType = providerType;
    }

    public int RegistrationIndex { get; }

    public IntrinsicDescriptorProviderRegistrationKind Kind { get; }

    public Type? ProviderType { get; }

    public bool CanValidateBeforeProviderBuild => ProviderType != null;
}
