namespace UniversalToolchain.Wist;

/// <summary>
///     Selects a shipped Wist dialect profile.
/// </summary>
public enum WistPreset
{
    /// <summary>
    ///     Restricted arithmetic/formula surface backed by the shipped pricing-restricted profile.
    /// </summary>
    RestrictedArithmetic,

    /// <summary>
    ///     Full native Wist surface for trusted input. This preset is not a security sandbox.
    /// </summary>
    FullNative
}
