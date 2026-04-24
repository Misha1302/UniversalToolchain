namespace UniversalToolchain.Features.Abstractions;

/// <summary>
///     Identifies a user-facing language capability.
/// </summary>
public readonly record struct LanguageFeatureId(string Value)
{
    /// <summary>
    ///     Returns the raw feature identifier value.
    /// </summary>
    public override string ToString()
    {
        return Value;
    }
}
