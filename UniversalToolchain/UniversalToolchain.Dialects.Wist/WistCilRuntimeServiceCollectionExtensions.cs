using System.Reflection.Emit;
using BasicCilCompiler.Execution;
using BasicCore.ExecutorWrapper;
using BytecodeDynamicMethodsCompiler.Compilers;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace UniversalToolchain.Dialects.Wist;

internal static class WistCilRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddWistCilRuntimeServices(this IServiceCollection services)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        services.TryAddTransient<AbstractMethodsCompilerImpl>();
        services.TryAddTransient<Func<IExecutor<DynamicMethod>>>(_ => () => new DynamicMethodExecutor());
        return services;
    }
}
