using BasicCore.Contracts;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.Dialects.Wist;

internal static class WistModuleContractServiceCollectionExtensions
{
    public static IServiceCollection AddWistModuleContractPipelineServices(
        this IServiceCollection services,
        ModuleContractPipelineOptions options,
        IModuleContractDiagnosticSink diagnosticSink)
    {
        services = services.ArgNotNull();
        options = options.ArgNotNull();
        diagnosticSink = diagnosticSink.ArgNotNull();
        if (options.Enabled && diagnosticSink is NullModuleContractDiagnosticSink)
            throw new ArgumentException("Enabled module-contract verification requires an observable diagnostic sink.", nameof(diagnosticSink));

        services.TryAddSingleton(options);
        services.TryAddSingleton<IModuleContractDiagnosticSink>(diagnosticSink);
        services.TryAddSingleton<IBytecodeObservedEmissionReader, BytecodeObservedEmissionReader>();
        services.TryAddSingleton<IBytecodeVerifier, BytecodeVerifier>();
        services.TryAddSingleton<IAirVerifier, AirVerifier>();
        services.TryAddSingleton<IOptimizerAirValidationHook, OptimizerAirValidationHook>();
        services.TryAddSingleton<IModuleContractDiagnosticPolicy, ModuleContractDiagnosticPolicy>();
        services.TryAddSingleton<PipelineEffectVerifier>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompilerFactVerifierRuleProvider, CoreCompilerFactVerifierRuleProvider>());
        services.TryAddSingleton<CompilerFactVerifierRegistry>(provider =>
            new CompilerFactVerifierRegistry(provider.GetServices<ICompilerFactVerifierRuleProvider>()));
        services.TryAddSingleton<ICompilerStageFactSeedProvider, CoreCompilerStageFactSeedProvider>();
        services.TryAddSingleton<ModuleContractSelectionBuilder>();
        services.TryAddSingleton<ISelectedModuleContractTableProvider>(provider => new SelectedModuleContractTableProvider(
            provider.GetRequiredService<ModuleContractPipelineOptions>().EnforcementPolicy,
            provider.GetRequiredService<ModuleContractSelectionBuilder>()));
        services.TryAddSingleton<IBackendCapabilitySelectionFactory>(provider => new BackendCapabilitySelectionFactory(
            provider.GetRequiredService<ModuleContractPipelineOptions>().BackendPolicy));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICompilationPipelineObserver, ModuleContractPipelineObserver>());

        return services;
    }
}
