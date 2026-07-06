namespace UniversalToolchain.Wist;

/// <summary>
///     Selects a shipped Wist dialect profile.
/// </summary>
public enum WistPreset
{
    /// <summary>
    ///     Restricted arithmetic/formula dialect surface backed by the shipped pricing-restricted profile.
    ///     This is the recommended first-contact preset for formulas.
    /// </summary>
    RestrictedArithmetic,

    /// <summary>
    ///     Full native Wist preview profile. This exposes the broad Wist language/runtime surface and is not a sandbox.
    /// </summary>
    FullNativePreview,

    /// <summary>
    ///     Compatibility alias for <see cref="RestrictedArithmetic"/>.
    /// </summary>
    SafeFormulas,

    /// <summary>
    ///     Compatibility alias for <see cref="FullNativePreview"/> in this preview.
    ///     This is not a separate stable business-rules runtime.
    /// </summary>
    BusinessRules,

    /// <summary>
    ///     Compatibility alias for <see cref="FullNativePreview"/>. Do not use this preset for untrusted input.
    /// </summary>
    FullTrusted
}
