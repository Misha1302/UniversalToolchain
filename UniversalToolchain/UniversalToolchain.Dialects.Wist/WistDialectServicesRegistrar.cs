using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Registers the Wist dialect integration layer into a service collection.
/// </summary>
internal sealed class WistDialectServicesRegistrar : IDialectServicesRegistrar
{
    public void Register(IServiceCollection services)
    {
        services = services.ArgNotNull();

        services.AddWistDialectServices();
    }
}