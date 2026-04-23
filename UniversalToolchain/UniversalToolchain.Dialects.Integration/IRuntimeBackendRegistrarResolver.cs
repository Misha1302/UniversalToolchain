namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Resolves backend runtime registrars from selected backend manifest entries.
/// </summary>
public interface IRuntimeBackendRegistrarResolver
{
    /// <summary>
    ///     Resolves the runtime registrar declared by the selected backend manifest entry.
    /// </summary>
    IDialectBackendRuntimeRegistrar Resolve(RuntimeComponentManifestEntry backendEntry);
}
