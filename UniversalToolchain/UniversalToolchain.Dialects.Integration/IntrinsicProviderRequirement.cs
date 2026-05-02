using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Describes an intrinsic descriptor provider requirement declared by a runtime activation module.
/// </summary>
public sealed record IntrinsicProviderRequirement
{
    public IntrinsicProviderRequirement(Type moduleType, Type providerType)
    {
        ModuleType = moduleType.NotNull(nameof(moduleType));
        ProviderType = providerType.NotNull(nameof(providerType));
    }

    public Type ModuleType { get; }

    public Type ProviderType { get; }
}