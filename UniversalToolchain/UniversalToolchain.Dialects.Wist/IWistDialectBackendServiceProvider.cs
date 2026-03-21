using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

public interface IWistDialectBackendServiceProvider
{
    DialectBackendId BackendId { get; }

    IReadOnlyList<string> SupportedIntrinsics { get; }

    void RegisterRuntime(IServiceCollection services, WistDialectBackendConfiguration configuration);
}
