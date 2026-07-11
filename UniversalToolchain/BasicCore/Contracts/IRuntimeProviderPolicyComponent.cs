namespace BasicCore.Contracts;

/// <summary>
///     Backend/runtime composition-owned allowlist for execution-scoped runtime call providers.
///     AIR may request a provider through a C# call descriptor, but it does not grant itself permission.
/// </summary>
public interface IRuntimeProviderPolicyComponent : IBackendPipelineComponent
{
    IReadOnlyCollection<Type> AllowedRuntimeProviderTypes { get; }
}
