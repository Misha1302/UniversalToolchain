namespace DependencyInjection;

public static class AutoRegistration
{
    /// <summary>
    ///     Automatically registers all modules and services in the assembly
    /// </summary>
    public static IServiceCollection AddAutoRegisteredServices(
        this IServiceCollection services,
        params IEnumerable<Assembly> assemblies)
    {
        // ReSharper disable PossibleMultipleEnumeration
        var types = !assemblies.Any() ? TypesFinder.AllTypes : assemblies.SelectMany(x => x.GetTypes());

        RegisterServices(services, types);

        return services;
    }

    /// <summary>
    ///     Registers all services marked with AutoRegisterServiceAttribute
    /// </summary>
    private static void RegisterServices(IServiceCollection services, IEnumerable<Type> types)
    {
        var serviceTypes = types
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Select(t =>
            {
                try
                {
                    return (Type: t, Attr: t.GetCustomAttribute<AutoRegisterServiceAttribute>());
                }
                catch
                {
                    return default;
                }
            })
            .Where(x => x is { Type: not null, Attr: not null })
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

        return null;
    }
}