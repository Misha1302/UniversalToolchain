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
        AddSharedWistDialectServices(services);
        AddMinimalCompositionServices(services);
        return services;
    }

    public static IServiceCollection AddWistDialectServicesLegacy(this IServiceCollection services)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        AddWistDialectServicesMinimal(services);
        AddLegacyCompatibilityServices(services);
        return services;
    }

    private static void AddSharedWistDialectServices(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectBackendRuntimeRegistrar, WistCilDialectBackendServiceProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectBackendRuntimeRegistrar, WistInterpreterDialectBackendServiceProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWistDialectBackendServiceProvider, WistCilDialectBackendServiceProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWistDialectBackendServiceProvider, WistInterpreterDialectBackendServiceProvider>());

        services.TryAddSingleton<SelectedRuntimePlanResolver>();
        services.TryAddSingleton<RuntimeArtifactLocatorOptions>();
        services.TryAddSingleton<IRuntimeManifestFileLocator>(provider =>
            new DefaultRuntimeManifestFileLocator(provider.GetRequiredService<RuntimeArtifactLocatorOptions>()));
        services.TryAddSingleton<IRuntimeAssemblyLocator>(provider =>
            new DefaultRuntimeAssemblyLocator(provider.GetRequiredService<RuntimeArtifactLocatorOptions>()));
        services.TryAddSingleton<IRuntimeManifestSerializer, RuntimeManifestJsonSerializer>();
        services.TryAddSingleton<IRuntimeComponentCatalog, FileBasedRuntimeComponentCatalog>();
        services.TryAddSingleton<IRuntimeComponentTypeLoader, DefaultRuntimeComponentTypeLoader>();
        services.TryAddSingleton<IRuntimeKnownBackendsProvider, RuntimeKnownBackendsProvider>();
        services.TryAddSingleton<IWistKnownBackendsProvider, WistKnownBackendsProvider>();
        services.TryAddSingleton<DialectIntrinsicPolicyResolver>();
        services.TryAddSingleton<IDialectCompiledDialectBuildPlanBuilder, DialectCompiledDialectBuildPlanBuilder>();
        services.TryAddSingleton<WistDialectExecutionConfigurationBuilder>();
        services.TryAddSingleton<WistDialectServiceProviderFactory>();
        services.TryAddSingleton<WistDialectExecutionWorkflow>();
    }

    private static void AddMinimalCompositionServices(IServiceCollection services)
    {
        // minimal path intentionally avoids runtime descriptor discovery services.
    }

    private static void AddLegacyCompatibilityServices(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, WistDialectRuntimeDescriptorProvider>());
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
