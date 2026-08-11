using System.Reflection;
using UniversalToolchain.Wist.LanguagePack;

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
        WistDialectSource.FromShippedPreset(WistLanguageDefinitions.PricingRestrictedId);

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

    /// <summary>
    /// Controls source text retained by durable <see cref="WistProgramMetadata"/>.
    /// Full preserves the alpha compatibility behavior; use HashAndIdentity or None for sensitive formulas.
    /// </summary>
    public WistSourceRetentionPolicy SourceRetention { get; set; } = WistSourceRetentionPolicy.Full;

    /// <summary>
    /// Controls whether expected-failure results expose raw developer exception information.
    /// Safe is the production default and does not expose exception objects.
    /// </summary>
    public WistDiagnosticExposure DiagnosticExposure { get; set; } = WistDiagnosticExposure.Safe;

    internal WistVerificationPolicy VerificationPolicy { get; set; } = WistVerificationPolicy.P3Always;

    public static WistEngineOptions FromPresetId(string presetId) => new()
    {
        DialectSource = WistDialectSource.FromShippedPreset(presetId),
        BackendId = WistLanguageDefinitions.GetDefaultBackendId(presetId)
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
