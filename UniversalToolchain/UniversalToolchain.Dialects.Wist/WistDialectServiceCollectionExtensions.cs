using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public static class WistDialectServiceCollectionExtensions
{
    public static IServiceCollection AddWistDialectServices(this IServiceCollection services) => AddWistDialectServicesMinimal(services);

    public static IServiceCollection AddWistDialectServicesMinimal(this IServiceCollection services)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        services.AddDialectDslDefaultComposition();
        AddMinimalWistDialectServices(services);
        return services;
    }

    public static IServiceCollection AddWistDialectServicesLegacy(this IServiceCollection services)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        services.AddWistDialectServicesMinimal();
        AddLegacyCompatibilityServices(services);
        return services;
    }

    private static void AddMinimalWistDialectServices(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectBackendRuntimeRegistrar, WistCilDialectBackendServiceProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectBackendRuntimeRegistrar, WistInterpreterDialectBackendServiceProvider>());

        services.TryAddSingleton<SelectedRuntimePlanResolver>();
        services.TryAddSingleton(new RuntimeArtifactLocatorOptions());
        services.TryAddSingleton<IRuntimeManifestFileLocator, DefaultRuntimeManifestFileLocator>();
        services.TryAddSingleton<IRuntimeAssemblyLocator, DefaultRuntimeAssemblyLocator>();
        services.TryAddSingleton<IRuntimeManifestSerializer, RuntimeManifestJsonSerializer>();
        services.TryAddSingleton<IRuntimeComponentCatalog, FileBasedRuntimeComponentCatalog>();
        services.TryAddSingleton<IRuntimeComponentTypeLoader, DefaultRuntimeComponentTypeLoader>();
        services.TryAddSingleton<IRuntimeKnownBackendsProvider, RuntimeKnownBackendsProvider>();

        services.TryAddSingleton<DialectIntrinsicPolicyResolver>();
        services.TryAddSingleton<IDialectCompiledDialectBuildPlanBuilder, DialectCompiledDialectBuildPlanBuilder>();
        services.TryAddSingleton<WistDialectExecutionConfigurationBuilder>();
        services.TryAddSingleton<WistDialectServiceProviderFactory>();
        services.TryAddSingleton<WistDialectExecutionWorkflow>();
    }

    private static void AddLegacyCompatibilityServices(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, CatalogBackedDialectRuntimeDescriptorProvider>());
        services.TryAddSingleton(static provider => DialectRuntimeDescriptorRegistryFactory.BuildFromProviders(provider.GetServices<IDialectRuntimeDescriptorProvider>()));
        services.TryAddSingleton<IDialectRuntimeCompositionResolver, DialectRuntimeCompositionResolver>();
        services.TryAddSingleton(static provider => new DialectFrameworkCompositionWorkflow(
            provider.GetRequiredService<DialectDslCompiler>(),
            provider.GetRequiredService<IDialectCompiledDialectBuildPlanBuilder>(),
            provider.GetRequiredService<IDialectRuntimeCompositionResolver>()));
        services.TryAddSingleton<LegacyWistDialectCompositionService>();
    }
}
