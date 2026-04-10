using System.Reflection;
using BasicCore.Contracts;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDslFrontendServiceCollectionExtensions
{
    private static readonly MethodInfo AddCoreRuntimeInfrastructureMethod = ResolveAddCoreRuntimeInfrastructureMethod();

    public static IServiceCollection AddDialectDslFrontendCompilerServices(
        this IServiceCollection services,
        DialectDslFrontendModule frontendModule)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        if (frontendModule == null)
            Thrower.ArgumentNull(nameof(frontendModule));

        _ = (IServiceCollection)(AddCoreRuntimeInfrastructureMethod.Invoke(null, [services])
            ?? throw new InvalidOperationException("Core runtime infrastructure registration returned null."));

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
            throwOnError: true)
            ?? throw new InvalidOperationException("Core runtime service collection extensions type was not found.");

        return extensionType.GetMethod(
                   "AddCoreRuntimeInfrastructure",
                   BindingFlags.Public | BindingFlags.Static,
                   [typeof(IServiceCollection)])
               ?? throw new InvalidOperationException("AddCoreRuntimeInfrastructure(IServiceCollection) was not found.");
    }
}
