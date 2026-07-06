using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Core.Binding.Handlers;

namespace UniversalToolchain.Dialects.Core.ServiceCollection;

public static class DialectSemanticBindingServiceCollectionExtensions
{
    public static IServiceCollection AddDialectSemanticBinding(this IServiceCollection services)
    {
        services = services.ArgNotNull();

        services.TryAddSingleton(static provider => new DialectDirectiveHandlerRegistry(provider.GetServices<IDialectDirectiveHandler>()));
        return services;
    }

    public static IServiceCollection AddDialectSemanticBindingBuiltIns(this IServiceCollection services)
    {
        services = services.ArgNotNull();

        services.AddDialectSemanticBinding();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectDirectiveHandler, ModuleDirectiveHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectDirectiveHandler, BackendDirectiveHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectDirectiveHandler, IntrinsicDirectiveHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectDirectiveHandler, OptimizerDirectiveHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectDirectiveHandler, SecurityDirectiveHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectDirectiveHandler, CapabilityDirectiveHandler>());
        return services;
    }
}
