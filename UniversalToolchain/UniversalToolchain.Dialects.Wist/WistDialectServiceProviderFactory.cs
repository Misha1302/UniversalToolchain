using AbstractIrConverters;
using BasicCore.Contracts;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicCore.Core;
using BytecodeDynamicMethodsCompiler.Compilers;
using IntermediateRepresentationAbstractions;
using System.Reflection.Emit;
using DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
/// Builds a real Wist runtime service provider from resolved dialect execution configuration.
/// </summary>
public sealed class WistDialectServiceProviderFactory
{
    public IServiceProvider Create(WistDialectExecutionConfiguration configuration)
    {
        if (configuration == null)
        {
            Thrower.ArgumentNull(nameof(configuration));
        }

        var services = new ServiceCollection();
        services.AddWistCoreServices();

        RegisterModules(services, configuration.FrontendModules, typeof(IFrontendCoreModule), Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);
        RegisterModules(services, configuration.IrModules, typeof(IIRProcessingModule), Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient);
        RegisterModules(services, configuration.Optimizers, typeof(IIRProcessingModule), Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient);
        RegisterCores(services, configuration);

        return services.BuildServiceProvider();
    }

    private static void RegisterModules(IServiceCollection services, IEnumerable<Type> types, Type serviceType, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime)
    {
        foreach (var type in types.OrderBy(x => x.FullName, StringComparer.Ordinal))
        {
            services.Add(new ServiceDescriptor(serviceType, type, lifetime));
        }
    }

    private static void RegisterCores(IServiceCollection services, WistDialectExecutionConfiguration configuration)
    {
        foreach (var backend in configuration.EnabledBackends.OrderBy(x => x))
        {
            switch (backend)
            {
                case DialectBackendTarget.Cil:
                    RegisterCore<DynamicMethod>(services, provider => CreateCompilerCore(provider, configuration));
                    break;

                case DialectBackendTarget.Interpreter:
                    RegisterCore<IAbstractIR>(services, provider => CreateInterpreterCore(provider, configuration));
                    break;

                default:
                    Thrower.InvalidOpEx($"Unsupported backend '{backend}'.");
                    break;
            }
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

    private static BasicCoreImpl<DynamicMethod> CreateCompilerCore(IServiceProvider provider, WistDialectExecutionConfiguration configuration)
    {
        return new BasicCoreImpl<DynamicMethod>(
            provider.GetRequiredService<Func<ILexer>>(),
            provider.GetRequiredService<Func<IParser>>(),
            provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
            provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
            () => new DialectIntrinsicPolicyCompiler<DynamicMethod>(
                provider.GetRequiredService<AbstractMethodsCompilerImpl>(),
                configuration.AllowedIntrinsics,
                configuration.ForbiddenIntrinsics),
            provider.GetRequiredService<Func<IExecutor<DynamicMethod>>>(),
            provider.GetServices<IFrontendCoreModule>().ToList(),
            provider.GetServices<IIRProcessingModule>().ToList(),
            []);
    }

    private static BasicCoreImpl<IAbstractIR> CreateInterpreterCore(IServiceProvider provider, WistDialectExecutionConfiguration configuration)
    {
        return new BasicCoreImpl<IAbstractIR>(
            provider.GetRequiredService<Func<ILexer>>(),
            provider.GetRequiredService<Func<IParser>>(),
            provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
            provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
            () => new DialectIntrinsicPolicyCompiler<IAbstractIR>(
                provider.GetRequiredService<AbstractIrToAbstractIrStub>(),
                configuration.AllowedIntrinsics,
                configuration.ForbiddenIntrinsics),
            provider.GetRequiredService<Func<IExecutor<IAbstractIR>>>(),
            provider.GetServices<IFrontendCoreModule>().ToList(),
            provider.GetServices<IIRProcessingModule>().ToList(),
            []);
    }
}
