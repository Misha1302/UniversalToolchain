using BasicCore.Contracts;
using DependencyInjection;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using ServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Builds a real Wist runtime service provider from resolved dialect execution configuration.
/// </summary>
public sealed class WistDialectServiceProviderFactory
{
    private readonly IReadOnlyDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar> _backendProviders;

    public WistDialectServiceProviderFactory(IEnumerable<IDialectBackendRuntimeRegistrar> backendProviders)
    {
        _backendProviders = CreateBackendProviderMap(backendProviders);
    }

    public IServiceProvider Create(WistDialectExecutionConfiguration configuration)
    {
        if (configuration == null)
            Thrower.ArgumentNull(nameof(configuration));

        var services = new ServiceCollection();
        services.AddWistCoreServices();

        RegisterModules(services, configuration.FrontendModules, typeof(IFrontendCoreModule), ServiceLifetime.Singleton);
        RegisterModules(services, configuration.IrModules, typeof(IIRProcessingModule), ServiceLifetime.Transient);
        RegisterModules(services, configuration.Optimizers, typeof(IIRProcessingModule), ServiceLifetime.Transient);
        RegisterBackendRuntimes(services, configuration);

        return services.BuildServiceProvider();
    }

    private static void RegisterModules(IServiceCollection services, IEnumerable<Type> types, Type serviceType, ServiceLifetime lifetime)
    {
        foreach (var type in types.OrderBy(x => x.FullName, StringComparer.Ordinal))
            services.Add(new ServiceDescriptor(serviceType, type, lifetime));
    }

    private void RegisterBackendRuntimes(IServiceCollection services, WistDialectExecutionConfiguration configuration)
    {
        foreach (var backend in configuration.BackendConfigurations.OrderBy(x => x.BackendDescriptor.BackendId))
        {
            if (!_backendProviders.TryGetValue(backend.BackendDescriptor.BackendId, out var backendProvider))
                Thrower.InvalidOpEx($"No backend runtime registrar is registered for backend '{backend.BackendDescriptor.CanonicalId}'.");

            backendProvider.RegisterRuntime(services, backend);
        }
    }

    private static IReadOnlyDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar> CreateBackendProviderMap(IEnumerable<IDialectBackendRuntimeRegistrar> backendProviders)
    {
        if (backendProviders == null)
            Thrower.ArgumentNull(nameof(backendProviders));

        var map = new SortedDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar>();
        foreach (var backendProvider in backendProviders
                     .Select(x => x.NotNull(nameof(backendProviders)))
                     .OrderBy(x => x.BackendId))
        {
            if (!map.TryAdd(backendProvider.BackendId, backendProvider))
                Thrower.InvalidOpEx($"Duplicate backend runtime registrar registration for backend '{backendProvider.BackendId.Value}'.");
        }

        return map;
    }
}
