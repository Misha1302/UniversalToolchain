using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Resolves validated dialect build plans against explicit runtime descriptors.
/// </summary>
public interface IDialectRuntimeCompositionResolver
{
    /// <summary>
    ///     Resolves a runtime composition from a validated build plan and explicit descriptor registry.
    /// </summary>
    /// <param name="buildPlan">Validated dialect build plan.</param>
    /// <param name="registry">Explicit descriptor registry.</param>
    /// <returns>Deterministic runtime composition description.</returns>
    DialectRuntimeComposition Resolve(DialectBuildPlan buildPlan, DialectRuntimeDescriptorRegistry registry);
}