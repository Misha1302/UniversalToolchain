using BasicCore.Capabilities;
using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public abstract class DialectBackendRuntimeRegistrarBase<TCompilationOutput> : IDialectBackendRuntimeRegistrar
{
    public abstract DialectBackendId BackendId { get; }

    public abstract IReadOnlyList<string> SupportedIntrinsics { get; }

    public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
    {
        services = services.ArgNotNull();

        configuration = configuration.ArgNotNull();

        RegisterBackendDefaults(services);

        var registration = new ToolchainBackendRuntimeRegistration(
            configuration.BackendDescriptor,
            provider => new ToolchainBackendRuntime(
                configuration.BackendDescriptor,
                CreateCore(provider, configuration)));

        services.AddSingleton(registration);
        services.AddSingleton<ToolchainBackendRuntime>(provider => registration.Resolve(provider));
        services.AddSingleton<ICoreRunnable>(provider => registration.Resolve(provider).Core);
        services.AddSingleton<ICoreOptimizedRunnable>(provider =>
            (ICoreOptimizedRunnable)registration.Resolve(provider).Core);
        services.AddSingleton<IExecutableGiver<TCompilationOutput>>(provider =>
            (IExecutableGiver<TCompilationOutput>)registration.Resolve(provider).Core);
        services.AddSingleton(provider => new WistDialectBackendRuntime(
            configuration.BackendDescriptor,
            registration.Resolve(provider).Core));
    }

    protected abstract void RegisterBackendDefaults(IServiceCollection services);

    protected abstract IAbstractIrCompiler<TCompilationOutput> ResolveBackendCompiler(IServiceProvider provider);

    protected abstract Func<IExecutor<TCompilationOutput>> ResolveExecutorFactory(IServiceProvider provider);

    protected virtual IReadOnlyList<IBackendPipelineComponent> GetBackendPipelineComponents(
        IServiceProvider provider,
        DialectBackendRuntimeConfiguration configuration) =>
        [];

    private BasicCoreImpl<TCompilationOutput> CreateCore(IServiceProvider provider, DialectBackendRuntimeConfiguration configuration)
    {
        var capabilitySetFactory = provider.GetRequiredService<IIntrinsicCapabilitySetFactory>();
        var backendOptimizers = configuration.OptimizerTypes
            .Select(type => (IAirOptimizer)provider.GetRequiredService(type))
            .ToList();
        var frontendModules = provider.GetServices<IFrontendCoreModule>().ToList();
        var pipelineObservers = provider.GetServices<ICompilationPipelineObserver>().ToList();
        var backendCompiler = ResolveBackendCompiler(provider);
        var executorFactory = ResolveExecutorFactory(provider);
        var backendPipelineComponents = GetBackendPipelineComponents(provider, configuration);

        return new BasicCoreImpl<TCompilationOutput>(
            provider.GetRequiredService<Func<ILexer>>(),
            provider.GetRequiredService<Func<IParser>>(),
            provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
            provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
            () => new DialectIntrinsicPolicyCompiler<TCompilationOutput>(
                backendCompiler,
                configuration.AllowedIntrinsics,
                configuration.ForbiddenIntrinsics,
                configuration.HasExplicitAllowList),
            executorFactory,
            frontendModules,
            backendOptimizers,
            [],
            capabilitySetFactory,
            pipelineObservers,
            backendPipelineComponents);
    }
}
