using System.Reflection;
using BasicCore.Contracts;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDslFrontendServiceCollectionExtensions
{
    private static readonly MethodInfo _addCoreRuntimeInfrastructureMethod = ResolveAddCoreRuntimeInfrastructureMethod();

    public static IServiceCollection AddDialectDslFrontendCompilerServices(
        this IServiceCollection services,
        DialectDslFrontendModule frontendModule)
    {
        services = services.ArgNotNull();

        frontendModule = frontendModule.ArgNotNull();

        var coreRuntimeInfrastructure = _addCoreRuntimeInfrastructureMethod.Invoke(null, [services]);
        if (coreRuntimeInfrastructure == null)
            Thrower.InvalidOpEx("Core runtime infrastructure registration returned null.");

        _ = (IServiceCollection)coreRuntimeInfrastructure;

        services.AddSingleton(frontendModule);
        services.AddSingleton<IFrontendCoreModule>(frontendModule);

        return services;
    }

    private static MethodInfo ResolveAddCoreRuntimeInfrastructureMethod()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
                               .FirstOrDefault(static assembly => string.Equals(
                                   assembly.GetName().Name,
                                   "UniversalToolchain.Dialects.Core",
                                   StringComparison.Ordinal))
                           ?? Assembly.Load("UniversalToolchain.Dialects.Core");

        var extensionType = coreAssembly.GetType(
            "UniversalToolchain.Dialects.Core.ServiceCollection.CoreRuntimeServiceCollectionExtensions",
            true);

        if (extensionType == null)
            Thrower.InvalidOpEx("Core runtime service collection extensions type was not found.");

        var method = extensionType.GetMethod(
            "AddCoreRuntimeInfrastructure",
            BindingFlags.Public | BindingFlags.Static,
            [typeof(IServiceCollection)]);

        if (method == null)
            Thrower.InvalidOpEx("AddCoreRuntimeInfrastructure(IServiceCollection) was not found.");

        return method;
    }
}