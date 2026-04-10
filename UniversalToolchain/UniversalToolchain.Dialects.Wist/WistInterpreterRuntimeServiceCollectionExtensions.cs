using AbstractIrConverters;
using BasicCore.ExecutorWrapper;
using BasicInterpreter;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace UniversalToolchain.Dialects.Wist;

internal static class WistInterpreterRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddWistInterpreterRuntimeServices(this IServiceCollection services)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        services.TryAddTransient<AbstractIrToAbstractIrStub>();
        services.TryAddTransient<Func<IExecutor<IAbstractIR>>>(_ => () => new InterpreterImpl());
        return services;
    }
}
