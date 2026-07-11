using System.Reflection;

namespace UniversalToolchain.Wist;

/// <summary>
///     Configures the public Wist facade.
/// </summary>
public sealed class WistEngineOptions
{
    /// <summary>
    ///     Gets or sets the legacy convenience preset used when <see cref="DialectSource" /> is not set.
    /// </summary>
    public WistPreset Preset { get; set; } = WistPreset.RestrictedArithmetic;

    /// <summary>
    ///     Gets or sets an open dialect source. Prefer this for custom dialect files or non-enum shipped presets.
    /// </summary>
    public WistDialectSource? DialectSource { get; set; }

    /// <summary>
    ///     Gets or sets the legacy convenience backend used when <see cref="BackendAlias" /> is not set.
    /// </summary>
    public WistBackend Backend { get; set; } = WistBackend.Compiler;

    /// <summary>
    ///     Gets or sets an open backend alias, for example "compiler", "interpreter", or a dialect-defined backend id.
    /// </summary>
    public string? BackendAlias { get; set; }

    /// <summary>
    ///     Gets the explicit host assembly allowlist exposed to CLR interop and type directives.
    ///     The runtime never scans the AppDomain or output directories.
    /// </summary>
    public IReadOnlyCollection<Assembly> AllowedAssemblies { get; set; } = Array.Empty<Assembly>();

    /// <summary>
    ///     Gets or sets host-owned preflight limits. These limits do not provide process isolation,
    ///     execution timeouts, or memory quotas.
    /// </summary>
    public WistResourceLimits ResourceLimits { get; set; } = new();

    /// <summary>
    /// Gets or sets optional compiler optimization routes. SSA is opt-in and experimental.
    /// </summary>
    public WistOptimizationOptions Optimization { get; set; } = new();

    public static WistEngineOptions FromPresetId(string presetId) => new()
    {
        DialectSource = WistDialectSource.FromShippedPreset(presetId)
    };

    public static WistEngineOptions FromDialectFile(string path) => new()
    {
        DialectSource = WistDialectSource.FromFile(path)
    };

    public static WistEngineOptions FromDialectText(string sourceText, string sourceName = "inline.wistdialect") => new()
    {
        DialectSource = WistDialectSource.FromText(sourceText, sourceName)
    };
}
