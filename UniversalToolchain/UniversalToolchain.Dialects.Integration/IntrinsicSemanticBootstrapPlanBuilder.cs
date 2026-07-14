using BasicCore.Contracts;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Builds intrinsic semantic bootstrap plans from service registrations before provider creation.
/// </summary>
public sealed class IntrinsicSemanticBootstrapPlanBuilder
{
    public IntrinsicSemanticBootstrapPlan Build(IServiceCollection services)
    {
        services = services.ArgNotNull();

        var providerRegistrations = services
            .Select(static (descriptor, index) => new RegistrationEntry(index, descriptor))
            .Where(static x => x.Descriptor.ServiceType == typeof(IIntrinsicDescriptorProvider))
            .Select(static x => ToProviderRegistration(x.Index, x.Descriptor))
            .OrderBy(x => x.RegistrationIndex)
            .ToList();

        var moduleTypes = GetRegisteredImplementationTypes(services, typeof(IFrontendCoreModule))
            .Concat(GetRegisteredImplementationTypes(services, typeof(IAirOptimizer)))
            .Distinct()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();

        var requirements = moduleTypes
            .SelectMany(static moduleType =>
                moduleType.GetCustomAttributes(typeof(IntrinsicDescriptorProviderAttribute), false)
                    .Cast<IntrinsicDescriptorProviderAttribute>()
                    .Select(attribute => new IntrinsicProviderRequirement(moduleType, attribute.ProviderType)))
            .OrderBy(x => x.ModuleType.FullName, StringComparer.Ordinal)
            .ThenBy(x => x.ProviderType.FullName, StringComparer.Ordinal)
            .ToList();

        return new IntrinsicSemanticBootstrapPlan(providerRegistrations, requirements);
    }

    private static IntrinsicDescriptorProviderRegistration ToProviderRegistration(int registrationIndex, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationType != null)
            return new IntrinsicDescriptorProviderRegistration(
                registrationIndex,
                IntrinsicDescriptorProviderRegistrationKind.ImplementationType,
                descriptor.ImplementationType);

        if (descriptor.ImplementationInstance != null)
            return new IntrinsicDescriptorProviderRegistration(
                registrationIndex,
                IntrinsicDescriptorProviderRegistrationKind.ImplementationInstance,
                descriptor.ImplementationInstance.GetType());

        if (descriptor.ImplementationFactory != null)
            return new IntrinsicDescriptorProviderRegistration(
                registrationIndex,
                IntrinsicDescriptorProviderRegistrationKind.Factory,
                null);

        Thrower.InvalidOpEx(
            $"Intrinsic descriptor provider registration at index {registrationIndex} has no implementation type, instance, or factory.");
        return null!;
    }

    private static IReadOnlyList<Type> GetRegisteredImplementationTypes(IServiceCollection services, Type serviceType)
    {
        return services
            .Where(x => x.ServiceType == serviceType)
            .Select(x => x.ImplementationType ?? x.ImplementationInstance?.GetType())
            .Where(static x => x != null)
            .Cast<Type>()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private sealed record RegistrationEntry(int Index, ServiceDescriptor Descriptor);
}