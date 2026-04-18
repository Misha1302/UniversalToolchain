using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Registers Wist dialect orchestration services without runtime catalog or resolution policy services.
/// </summary>
public static class WistDialectCoreServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the core services required to compile dialect definitions and orchestrate Wist execution workflows.
    /// </summary>
    public static IServiceCollection AddWistDialectCoreServices(this IServiceCollection services)
    {
        services = services.ArgNotNull();

        services.AddDialectDslDefaultComposition();

        services.TryAddSingleton<SelectedRuntimePlanResolver>();
        services.TryAddSingleton<DialectIntrinsicPolicyResolver>();
        services.TryAddSingleton<IDialectCompiledDialectBuildPlanBuilder, DialectCompiledDialectBuildPlanBuilder>();
        services.TryAddSingleton<WistDialectExecutionConfigurationBuilder>();
        services.TryAddSingleton<WistDialectServiceProviderFactory>();
        services.TryAddSingleton<WistDialectExecutionWorkflow>();

        return services;
    }
}