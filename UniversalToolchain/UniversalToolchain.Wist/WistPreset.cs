namespace UniversalToolchain.Wist;

/// <summary>
///     Selects a shipped Wist dialect profile through product-oriented names.
/// </summary>
public enum WistPreset
{
    /// <summary>
    ///     Restricted pricing/formula-oriented dialect surface.
    /// </summary>
    SafeFormulas,

    /// <summary>
    ///     General business-rule oriented dialect surface.
    /// </summary>
    BusinessRules,

    /// <summary>
    ///     Full trusted Wist profile. Do not use this preset for untrusted input.
    /// </summary>
    FullTrusted
}