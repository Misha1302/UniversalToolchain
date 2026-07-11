using System.Reflection;
using UniversalToolchain.Ssa.Optimization;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Host-owned runtime services that must not be inferred from the process or file system.
/// </summary>
public sealed class WistRuntimeServiceOptions
{
    /// <summary>
    ///     Gets the explicit assembly allowlist available to CLR type and method resolution.
    /// </summary>
    public IReadOnlyCollection<Assembly> AllowedAssemblies { get; init; } = Array.Empty<Assembly>();

    /// <summary>
    /// Gets execution-scoped configuration for the optional SSA optimizer module.
    /// </summary>
    public SsaRuntimeExecutionOptions SsaExecution { get; init; } = SsaRuntimeExecutionOptions.RequireDefault;

    /// <summary>
    /// Gets the report sink used by the host facade to observe the actual SSA route.
    /// </summary>
    public ISsaRouteReportSink SsaReportSink { get; init; } = NullSsaRouteReportSink.Instance;
}
