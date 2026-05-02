using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Core.Groups;
using UniversalToolchain.Dialects.Frontend.Composition;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist.Groups;

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

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectGroupProvider, WistDialectGroupProvider>());
        services.TryAddSingleton<IDialectGroupCatalog, CompositeDialectGroupCatalog>();
        services.TryAddSingleton<DialectGroupExpander>();
        services.TryAddSingleton<SelectedRuntimePlanResolver>();
        services.TryAddSingleton<IDialectBackendIntrinsicPolicyResolver, DialectIntrinsicPolicyResolver>();
        services.TryAddSingleton<IWistRequiredInfrastructureModulesProvider, WistRequiredInfrastructureModulesProvider>();
        services.TryAddSingleton<SelectedRuntimeModuleClassifier>();
        services.TryAddSingleton<SelectedRuntimeExecutionShapeBuilder>();
        services.TryAddSingleton<DialectBackendRuntimeConfigurationBuilder>();
        services.TryAddSingleton<IntrinsicSemanticBootstrapPlanBuilder>();
        services.TryAddSingleton<IntrinsicSemanticBootstrapPreProviderValidator>();
        services.TryAddSingleton<IntrinsicSemanticBootstrapRuntimeValidator>();
        services.TryAddSingleton<IDialectCompiledDialectBuildPlanBuilder, DialectCompiledDialectBuildPlanBuilder>();
        services.TryAddSingleton<WistDialectExecutionConfigurationBuilder>();
        services.TryAddSingleton<WistDialectServiceProviderFactory>();
        services.TryAddSingleton<WistDialectExecutionWorkflow>();

        return services;
    }
}