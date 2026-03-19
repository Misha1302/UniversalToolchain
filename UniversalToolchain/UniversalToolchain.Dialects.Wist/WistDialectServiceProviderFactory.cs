using System.Reflection.Emit;
using AbstractIrConverters;
using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BytecodeDynamicMethodsCompiler.Compilers;
using DependencyInjection;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using ServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Builds a real Wist runtime service provider from resolved dialect execution configuration.
/// </summary>
public sealed class WistDialectServiceProviderFactory
{
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

    private static void RegisterBackendRuntimes(IServiceCollection services, WistDialectExecutionConfiguration configuration)
    {
        foreach (var backend in configuration.BackendConfigurations.OrderBy(x => x.BackendDescriptor.BackendId))
        {
            if (backend.BackendDescriptor.BackendId == WistDialectBackendIds.Cil)
            {
                RegisterCore<DynamicMethod>(services, provider => CreateCompilerCore(provider, backend));
                services.AddTransient(provider => new WistDialectBackendRuntime(backend.BackendDescriptor, CreateCompilerCore(provider, backend)));
                continue;
            }

            if (backend.BackendDescriptor.BackendId == WistDialectBackendIds.Interpreter)
            {
                RegisterCore<IAbstractIR>(services, provider => CreateInterpreterCore(provider, backend));
                services.AddTransient(provider => new WistDialectBackendRuntime(backend.BackendDescriptor, CreateInterpreterCore(provider, backend)));
                continue;
            }

            Thrower.InvalidOpEx($"Unsupported backend '{backend.BackendDescriptor.CanonicalId}'.");
        }
    }

    private static void RegisterCore<TCompilationOutput>(
        IServiceCollection services,
        Func<IServiceProvider, BasicCoreImpl<TCompilationOutput>> factory)
    {
        services.AddTransient<ICoreRunnable>(provider => factory(provider));
        services.AddTransient<ICoreOptimizedRunnable>(provider => factory(provider));
        services.AddTransient<IExecutableGiver<TCompilationOutput>>(provider => factory(provider));
    }

    private static BasicCoreImpl<DynamicMethod> CreateCompilerCore(IServiceProvider provider, WistDialectBackendConfiguration backend)
    {
        return new BasicCoreImpl<DynamicMethod>(
            provider.GetRequiredService<Func<ILexer>>(),
            provider.GetRequiredService<Func<IParser>>(),
            provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
            provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
            () => new DialectIntrinsicPolicyCompiler<DynamicMethod>(
                provider.GetRequiredService<AbstractMethodsCompilerImpl>(),
                backend.AllowedIntrinsics,
                backend.ForbiddenIntrinsics,
                backend.HasExplicitAllowList),
            provider.GetRequiredService<Func<IExecutor<DynamicMethod>>>(),
            provider.GetServices<IFrontendCoreModule>().ToList(),
            provider.GetServices<IIRProcessingModule>().ToList(),
            []);
    }

    private static BasicCoreImpl<IAbstractIR> CreateInterpreterCore(IServiceProvider provider, WistDialectBackendConfiguration backend)
    {
        return new BasicCoreImpl<IAbstractIR>(
            provider.GetRequiredService<Func<ILexer>>(),
            provider.GetRequiredService<Func<IParser>>(),
            provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
            provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
            () => new DialectIntrinsicPolicyCompiler<IAbstractIR>(
                provider.GetRequiredService<AbstractIrToAbstractIrStub>(),
                backend.AllowedIntrinsics,
                backend.ForbiddenIntrinsics,
                backend.HasExplicitAllowList),
            provider.GetRequiredService<Func<IExecutor<IAbstractIR>>>(),
            provider.GetServices<IFrontendCoreModule>().ToList(),
            provider.GetServices<IIRProcessingModule>().ToList(),
            []);
    }
}
