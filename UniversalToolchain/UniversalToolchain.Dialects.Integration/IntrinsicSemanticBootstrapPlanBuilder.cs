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

        var providerTypes = services
            .Where(static x => x.ServiceType == typeof(IIntrinsicDescriptorProvider))
            .Select(static x => x.ImplementationType ?? x.ImplementationInstance?.GetType())
            .Where(static x => x != null)
            .Cast<Type>()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();

        var moduleTypes = GetRegisteredImplementationTypes(services, typeof(IFrontendCoreModule))
            .Concat(GetRegisteredImplementationTypes(services, typeof(IIRProcessingModule)))
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

        return new IntrinsicSemanticBootstrapPlan(providerTypes, requirements);
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
}
