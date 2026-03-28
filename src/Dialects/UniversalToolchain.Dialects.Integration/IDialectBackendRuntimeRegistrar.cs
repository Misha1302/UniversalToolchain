using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public interface IDialectBackendRuntimeRegistrar
{
    DialectBackendId BackendId { get; }

    IReadOnlyList<string> SupportedIntrinsics { get; }

    void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration);
}