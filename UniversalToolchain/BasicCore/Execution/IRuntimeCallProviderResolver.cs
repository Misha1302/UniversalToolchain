namespace BasicCore.Execution;

public interface IRuntimeCallProviderResolver
{
    /// <summary>
    ///     Resolves a session-scoped runtime call provider instance.
    ///     TODO: validate provider types against runtime composition-selected provider contracts.
    /// </summary>
    object GetRequiredProvider(Type providerType);
}