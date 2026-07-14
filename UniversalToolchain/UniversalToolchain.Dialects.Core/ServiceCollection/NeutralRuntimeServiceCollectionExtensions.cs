using BasicCore.Registration;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Core.ServiceCollection;

/// <summary>
///     Registers runtime services that are independent of concrete frontend and backend implementations.
/// </summary>
public static class NeutralRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddNeutralRuntimeInfrastructure(this IServiceCollection services)
    {
        services = services.ArgNotNull();
        return services.AddCoreIntrinsicServices();
    }
}
