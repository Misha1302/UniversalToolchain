using ArithmeticModule;
using BasicCore.Contracts;
using CommentsModule;
using ConditionsModule;
using CSharpInteropModule;
using DependencyInjection;
using EqualityModule;
using ExceptionsManager;
using IdentifierModule;
using InternalPreprocessorLexemesModule;
using LabelsModule;
using LocalVariablesOptimizerModule;
using LoopsModule;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Abstractions;
using NativeMathModule;
using NumbersModule;
using ParametersSetterModule;
using ScopesModule;
using SemicolonAsNewLineModule;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;
using VariablesModule;
using WhitespacesModule;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Registers Wist-specific dialect services and descriptor catalogs.
/// </summary>
public static class WistDialectServiceCollectionExtensions
{

    /// <summary>
    ///     Registers only pre-selected Wist runtime components without assembly auto-discovery.
    /// </summary>
    public static IServiceCollection AddSelectedWistRuntimeServices(
        this IServiceCollection services,
        DialectResolvedRuntimeSelection selection,
        IEnumerable<IWistDialectBackendServiceProvider> backendProviders,
        DialectBuildPlan buildPlan)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        if (selection == null)
            Thrower.ArgumentNull(nameof(selection));

        if (backendProviders == null)
            Thrower.ArgumentNull(nameof(backendProviders));

        if (buildPlan == null)
            Thrower.ArgumentNull(nameof(buildPlan));

        services.AddWistCoreServices();

        foreach (var module in selection.OrderedModules)
        {
            if (module.IsFrontendModule)
                services.AddSingleton(typeof(IFrontendCoreModule), module.ImplementationType);

            if (module.IsIrProcessingModule)
                services.AddTransient(typeof(IIRProcessingModule), module.ImplementationType);
        }

        foreach (var optimizer in selection.EnabledOptimizers)
            services.AddTransient(typeof(IIRProcessingModule), optimizer.ImplementationType);

        var providerMap = backendProviders.ToDictionary(x => x.BackendId, x => x);
        foreach (var backend in selection.EnabledBackends.OrderBy(x => x.CanonicalId))
        {
            if (!providerMap.TryGetValue(backend.CanonicalId, out var backendProvider))
                Thrower.InvalidOpEx($"No Wist backend service provider is registered for backend '{backend.CanonicalId.Value}'.");

            var runtimeDescriptor = new RuntimeBackendDescriptor(backend.CanonicalId, backend.ImplementationType, backend.Aliases);
            var allowedIntrinsics = ResolveAllowedIntrinsics(buildPlan, backendProvider, backend.CanonicalId);
            var hasExplicitAllowList = buildPlan.IntrinsicDirectives.Any(x => x.Allowed && x.Target.Matches(backend.CanonicalId));
            var forbiddenIntrinsics = buildPlan.IntrinsicDirectives
                .Where(x => !x.Allowed && x.Target.Matches(backend.CanonicalId))
                .Select(x => x.Name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            var config = new WistDialectBackendConfiguration(runtimeDescriptor, allowedIntrinsics, forbiddenIntrinsics, hasExplicitAllowList);
            backendProvider.RegisterRuntime(services, config);
        }

        return services;
    }

    private static IReadOnlyList<string> ResolveAllowedIntrinsics(DialectBuildPlan buildPlan, IWistDialectBackendServiceProvider backendProvider, DialectBackendId backendId)
    {
        var hasExplicitAllowList = buildPlan.IntrinsicDirectives.Any(x => x.Allowed && x.Target.Matches(backendId));
        if (!hasExplicitAllowList)
            return backendProvider.SupportedIntrinsics.ToList();

        return buildPlan.IntrinsicDirectives
            .Where(x => x.Allowed && x.Target.Matches(backendId))
            .Select(x => x.Name)
            .Distinct(StringComparer.Ordinal)
            .Where(x => backendProvider.SupportedIntrinsics.Contains(x, StringComparer.Ordinal))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }

    public static IServiceCollection AddWistDialectServices(this IServiceCollection services)
    {
        if (services == null)
        {
            Thrower.ArgumentNull(nameof(services));
        }

        services.AddDialectDslDefaultComposition();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWistDialectBackendServiceProvider, WistCilDialectBackendServiceProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWistDialectBackendServiceProvider, WistInterpreterDialectBackendServiceProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, WistDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, ArithmeticDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, ConditionsDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, EqualityDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, NativeMathDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, NumbersDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, CommentsDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, IdentifierDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, InternalPreprocessorLexemesDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, SemicolonAsNewLineDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, WhitespacesDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, ParametersSetterDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, ScopesDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, VariablesDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, LabelsDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, LoopsDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, CSharpInteropDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, LocalVariablesOptimizerDialectRuntimeDescriptorProvider>());
        services.TryAddSingleton(static provider => DialectRuntimeDescriptorRegistryFactory.BuildFromProviders(provider.GetServices<IDialectRuntimeDescriptorProvider>()));
        services.TryAddSingleton<IDialectCompiledDialectBuildPlanBuilder, DialectCompiledDialectBuildPlanBuilder>();
        services.TryAddSingleton<IDialectRuntimeCompositionResolver, DialectRuntimeCompositionResolver>();
        services.TryAddSingleton(static provider => new DialectFrameworkCompositionWorkflow(
            provider.GetRequiredService<DialectDslCompiler>(),
            provider.GetRequiredService<IDialectCompiledDialectBuildPlanBuilder>(),
            provider.GetRequiredService<IDialectRuntimeCompositionResolver>()));
        services.TryAddSingleton<WistDialectExecutionConfigurationBuilder>();
        services.TryAddSingleton<WistDialectServiceProviderFactory>();
        services.TryAddSingleton<IDialectRuntimeCatalog>(_ => WistRuntimeCatalogFactory.Create());
        services.TryAddSingleton<DialectRuntimeSelectionResolver>();
        services.TryAddSingleton<DialectRuntimeProviderFactory>();
        services.TryAddSingleton<WistDialectExecutionWorkflow>();
        return services;
    }
}
