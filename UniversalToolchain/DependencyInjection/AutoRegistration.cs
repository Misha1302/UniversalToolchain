using System.Reflection;
using AssemblyFinder;
using BasicCore;
using BasicCore.ParserWrapper;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection;

public static class AutoRegistration
{
    /// <summary>
    ///     Automatically registers all modules and services in the assembly
    /// </summary>
    public static IServiceCollection AddAutoRegisteredServices(
        this IServiceCollection services,
        params IReadOnlyList<Assembly> assemblies)
    {
        if (assemblies.Count == 0)
        {
            assemblies = TypesFinder.Assemblies;
        }

        foreach (var assembly in assemblies)
        {
            RegisterServices(services, assembly);
        }

        return services;
    }

    /// <summary>
    ///     Registers all services marked with AutoRegisterServiceAttribute
    /// </summary>
    private static void RegisterServices(IServiceCollection services, Assembly assembly)
    {
        var serviceTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => (Type: t, Attr: t.GetCustomAttribute<AutoRegisterServiceAttribute>()))
            .Where(x => x.Attr != null)
            .ToList();

        foreach (var (type, attribute) in serviceTypes)
        {
            var serviceType = attribute!.ServiceType ?? GetDefaultServiceType(type);
            if (serviceType == null) continue;

            switch (attribute.Lifetime)
            {
                case Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton:
                    services.AddSingleton(serviceType, type);
                    break;
                case Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient:
                    services.AddTransient(serviceType, type);
                    break;
                case Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped:
                    services.AddScoped(serviceType, type);
                    break;
            }
        }
    }

    /// <summary>
    ///     Gets default service type for auto-registration
    /// </summary>
    private static Type? GetDefaultServiceType(Type implementationType)
    {
        // Try to find the most appropriate interface
        var interfaces = implementationType.GetInterfaces();

        if (interfaces.Contains(typeof(IFrontendCoreModule)))
            return typeof(IFrontendCoreModule);

        if (interfaces.Contains(typeof(IAstNodeCreator)))
            return typeof(IAstNodeCreator);

        if (interfaces.Contains(typeof(IAstVisitor)))
            return typeof(IAstVisitor);

        if (interfaces.Contains(typeof(IIRProcessingModule)))
            return typeof(IIRProcessingModule);

        // Return the first interface, or null if none
        return interfaces.FirstOrDefault();
    }
}