using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
/// Wist compatibility view over the backend-neutral immutable runtime configuration.
/// </summary>
internal sealed class WistDialectExecutionConfiguration : ToolchainRuntimeConfiguration
{
    internal WistDialectExecutionConfiguration(
        string dialectName,
        IEnumerable<Type> frontendModules,
        IEnumerable<Type> irModules,
        IEnumerable<Type> optimizers,
        IEnumerable<DialectBackendRuntimeConfiguration> backendConfigurations,
        IEnumerable<RuntimeBackendDescriptor>? knownBackends = null,
        IEnumerable<Type>? requiredInfrastructureModules = null)
        : base(
            dialectName,
            frontendModules,
            irModules,
            optimizers,
            backendConfigurations,
            knownBackends,
            requiredInfrastructureModules)
    {
    }
}
