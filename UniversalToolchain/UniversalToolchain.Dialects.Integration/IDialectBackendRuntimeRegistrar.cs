using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Registers runtime services needed to execute one backend in a composed dialect host.
/// </summary>
public interface IDialectBackendRuntimeRegistrar
{
    DialectBackendId BackendId { get; }

    IReadOnlyList<string> SupportedIntrinsics { get; }

    void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration);
}
