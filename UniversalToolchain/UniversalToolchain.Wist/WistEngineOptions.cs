namespace UniversalToolchain.Wist;

/// <summary>
///     Configures the public Wist facade.
/// </summary>
public sealed class WistEngineOptions
{
    /// <summary>
    ///     Gets or sets the shipped Wist preset used by the engine.
    /// </summary>
    public WistPreset Preset { get; set; } = WistPreset.SafeFormulas;

    /// <summary>
    ///     Gets or sets the backend used by convenience evaluation APIs.
    /// </summary>
    public WistBackend Backend { get; set; } = WistBackend.Compiler;
}