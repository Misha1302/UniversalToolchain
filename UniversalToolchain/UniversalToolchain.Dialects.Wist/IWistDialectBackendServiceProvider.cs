using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public interface IWistDialectBackendServiceProvider : IDialectBackendRuntimeRegistrar
{
    void RegisterRuntime(IServiceCollection services, WistDialectBackendConfiguration configuration);
}
