namespace BasicCore.Execution;

/// <summary>
///     Provides compile-time external bindings layout for runtime execution environments.
/// </summary>
public interface IExternalBindingsLayoutProvider
{
    /// <summary>
    ///     Gets immutable external bindings layout.
    /// </summary>
    ExternalBindingsLayout ExternalBindingsLayout { get; }
}