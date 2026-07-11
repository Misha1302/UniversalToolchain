namespace UniversalToolchain.Wist;

/// <summary>
/// Configures optional public optimization routes without exposing internal compiler types.
/// </summary>
public sealed class WistOptimizationOptions
{
    /// <summary>
    /// Gets or sets the experimental SSA route configuration.
    /// </summary>
    public WistSsaOptions Ssa { get; set; } = new();

    internal WistOptimizationOptions SnapshotValidated() => new()
    {
        Ssa = (Ssa ?? throw new ArgumentNullException(nameof(Ssa))).SnapshotValidated()
    };
}

/// <summary>
/// Controls how the experimental verifier-gated SSA route participates in compilation.
/// </summary>
public enum WistSsaPolicy
{
    Disabled,
    Prefer,
    Require,
    Debug
}

/// <summary>
/// Controls the amount of observable SSA route detail returned by the facade.
/// </summary>
public enum WistSsaDiagnosticLevel
{
    Summary,
    Detailed
}

/// <summary>
/// Public, facade-owned SSA configuration. SSA remains experimental in preview releases.
/// </summary>
public sealed class WistSsaOptions
{
    /// <summary>
    /// Gets or sets the SSA route policy. The default keeps SSA disabled.
    /// </summary>
    public WistSsaPolicy Policy { get; set; } = WistSsaPolicy.Disabled;

    /// <summary>
    /// Gets or sets route-report detail. Detailed mode records stage trace entries.
    /// </summary>
    public WistSsaDiagnosticLevel DiagnosticLevel { get; set; } = WistSsaDiagnosticLevel.Summary;

    internal WistSsaOptions SnapshotValidated()
    {
        if (!Enum.IsDefined(Policy))
            throw new ArgumentOutOfRangeException(nameof(Policy), Policy, "Wist SSA policy is not defined.");
        if (!Enum.IsDefined(DiagnosticLevel))
            throw new ArgumentOutOfRangeException(nameof(DiagnosticLevel), DiagnosticLevel, "Wist SSA diagnostic level is not defined.");

        return new WistSsaOptions
        {
            Policy = Policy,
            DiagnosticLevel = DiagnosticLevel
        };
    }
}
