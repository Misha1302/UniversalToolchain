using System.Reflection;
using UniversalToolchain.Dialects.Wist.Presets;

namespace UniversalToolchain.Wist;

/// <summary>
///     Configures the public Wist facade.
/// </summary>
public sealed class WistEngineOptions
{
    /// <summary>
    ///     Gets or sets the exact dialect source used by the facade.
    /// </summary>
    public WistDialectSource DialectSource { get; set; } =
        WistDialectSource.FromShippedPreset("pricing-restricted");

    /// <summary>
    ///     Gets or sets the canonical backend identifier: "cil" or "interpreter".
    /// </summary>
    public string BackendId { get; set; } = "cil";

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

    public static WistEngineOptions FromPresetId(string presetId)
    {
        var preset = WistShippedDialectPresets.GetRequired(presetId);
        return new WistEngineOptions
        {
            DialectSource = WistDialectSource.FromShippedPreset(preset.Id),
            BackendId = preset.DefaultBackend
        };
    }

    public static WistEngineOptions FromDialectFile(string path) => new()
    {
        DialectSource = WistDialectSource.FromFile(path)
    };

    public static WistEngineOptions FromDialectText(string sourceText, string sourceName = "inline.wistdialect") => new()
    {
        DialectSource = WistDialectSource.FromText(sourceText, sourceName)
    };
}
