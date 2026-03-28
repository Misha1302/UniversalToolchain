using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public static class WistDialectServiceCollectionExtensions
{
    public static IServiceCollection AddWistDialectServices(this IServiceCollection services)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        services.AddDialectDslDefaultComposition();

        services.TryAddSingleton<SelectedRuntimePlanResolver>();
        services.TryAddSingleton(new RuntimeArtifactLocatorOptions());
        services.TryAddSingleton<IRuntimeManifestFileLocator, DefaultRuntimeManifestFileLocator>();
        services.TryAddSingleton<IRuntimeAssemblyLocator, DefaultRuntimeAssemblyLocator>();
        services.TryAddSingleton<IRuntimeManifestSerializer, RuntimeManifestJsonSerializer>();
        services.TryAddSingleton<IRuntimeComponentCatalog, FileBasedRuntimeComponentCatalog>();
        services.TryAddSingleton<IRuntimeAssemblyLoadStrategy, DefaultRuntimeAssemblyLoadStrategy>();
        services.TryAddSingleton<IRuntimeComponentTypeLoader, DefaultRuntimeComponentTypeLoader>();
        services.TryAddSingleton<IRuntimeKnownBackendsProvider, RuntimeKnownBackendsProvider>();

        services.TryAddSingleton<DialectIntrinsicPolicyResolver>();
        services.TryAddSingleton<IDialectCompiledDialectBuildPlanBuilder, DialectCompiledDialectBuildPlanBuilder>();
        services.TryAddSingleton<WistDialectExecutionConfigurationBuilder>();
        services.TryAddSingleton<WistDialectServiceProviderFactory>();
        services.TryAddSingleton<WistDialectExecutionWorkflow>();

        return services;
    }
}