using BasicCilCompiler.Execution;
using BasicCore.ExecutorWrapper;
using BytecodeDynamicMethodsCompiler.Compilers;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace UniversalToolchain.Dialects.Core.ServiceCollection;

/// <summary>
///     Registers the built-in compiler backend defaults.
/// </summary>
public static class CompilerBackendDefaultsServiceCollectionExtensions
{
    public static IServiceCollection AddCompilerBackendDefaults(this IServiceCollection services)
    {
        services = services.ArgNotNull();

        services.TryAddSingleton<CilIntrinsicRegistry>();
        services.TryAddTransient<AbstractMethodsCompilerImpl>();
        services.TryAddTransient<Func<IExecutor<CilCompilationOutput>>>(_ => () => new DynamicMethodExecutor());

        return services;
    }
}
