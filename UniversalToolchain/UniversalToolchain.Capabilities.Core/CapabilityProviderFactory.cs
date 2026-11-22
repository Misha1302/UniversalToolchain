using ExceptionsManager;
using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.Capabilities.Core;

public sealed class CapabilityProviderFactory
{
    public bool TryCreate(
        CapabilityProviderDescriptor descriptor,
        out object? provider,
        out ToolchainDiagnostic? diagnostic)
    {
        descriptor = descriptor.ArgNotNull();

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

        // GetConstructor(Type.EmptyTypes) binds Public|Instance only, so a non-null result is already public.
        // `!constructor.IsPublic` could never be true here; it was removed rather than left as a guard that
        // pretends to enforce something.
        //
        // The two rejections below are kept separate because they are different defects with different
        // remedies. Without the IsAbstract branch an abstract type falls through to Invoke, throws
        // MemberAccessException and is reported as "activation failed" - the code is the same, so only the
        // message distinguishes them, and "must declare a public parameterless constructor" is actively wrong
        // for an abstract type that does declare one.
        var constructor = providerType.GetConstructor(Type.EmptyTypes);
        if (constructor == null || providerType.IsAbstract)
        {
            diagnostic = CapabilityProviderTypeResolver.CreateInvalidProviderDiagnostic(
                descriptor.RuntimeComponentImplementationType,
                providerType,
                providerType.IsAbstract
                    ? "Capability provider type must not be abstract; an abstract type cannot be instantiated as a provider."
                    : "Capability provider type must declare a public parameterless constructor.");
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