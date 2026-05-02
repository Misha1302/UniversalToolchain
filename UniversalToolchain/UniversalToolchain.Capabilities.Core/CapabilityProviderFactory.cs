using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.Capabilities.Core;

public sealed class CapabilityProviderFactory
{
    public bool TryCreate(
        CapabilityProviderDescriptor descriptor,
        out object? provider,
        out ToolchainDiagnostic? diagnostic)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        provider = null;
        diagnostic = null;

        var providerType = descriptor.ProviderType;
        if (!CapabilityProviderTypeResolver.ImplementsKnownProviderInterface(providerType))
        {
            diagnostic = CapabilityProviderTypeResolver.CreateInvalidProviderDiagnostic(
                descriptor.RuntimeComponentImplementationType,
                providerType,
                "Capability provider type must implement at least one supported capability provider interface.");
            return false;
        }

        var constructor = providerType.GetConstructor(Type.EmptyTypes);
        if (constructor == null || !constructor.IsPublic || providerType.IsAbstract)
        {
            diagnostic = CapabilityProviderTypeResolver.CreateInvalidProviderDiagnostic(
                descriptor.RuntimeComponentImplementationType,
                providerType,
                "Capability provider type must declare a public parameterless constructor.");
            return false;
        }

        try
        {
            provider = constructor.Invoke([]);
            return true;
        }
        catch (Exception exception)
        {
            diagnostic = CapabilityProviderTypeResolver.CreateInvalidProviderDiagnostic(
                descriptor.RuntimeComponentImplementationType,
                providerType,
                $"Capability provider activation failed: {exception.GetType().FullName ?? exception.GetType().Name}.");
            return false;
        }
    }
}