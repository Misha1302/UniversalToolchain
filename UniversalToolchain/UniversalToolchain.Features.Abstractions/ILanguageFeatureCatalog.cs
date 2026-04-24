namespace UniversalToolchain.Features.Abstractions;

/// <summary>
///     Provides deterministic language feature metadata for a runtime surface.
/// </summary>
public interface ILanguageFeatureCatalog
{
    /// <summary>
    ///     Returns all known feature descriptors in deterministic order.
    /// </summary>
    IReadOnlyList<LanguageFeatureDescriptor> GetFeatures();

    /// <summary>
    ///     Tries to get the descriptor for the specified feature identifier.
    /// </summary>
    bool TryGetFeature(
        LanguageFeatureId featureId,
        out LanguageFeatureDescriptor? descriptor);
}
