namespace UniversalToolchain.Wist;

/// <summary>
///     Represents a non-throwing typed Wist compilation result for expected failures.
/// </summary>
public sealed class WistCompileResult<TDelegate>
    where TDelegate : Delegate
{
    private WistCompileResult(
        WistProgram<TDelegate>? program,
        WistFailureKind? failureKind,
        IReadOnlyList<WistDiagnostic> diagnostics,
        Exception? exception,
        WistOptimizationReport optimizationReport)
    {
        Program = program;
        FailureKind = failureKind;
        Diagnostics = diagnostics;
        Exception = exception;
        OptimizationReport = optimizationReport ?? throw new ArgumentNullException(nameof(optimizationReport));
    }

    public bool IsSuccess => Program != null;
    public WistProgram<TDelegate>? Program { get; }
    public WistFailureKind? FailureKind { get; }
    public IReadOnlyList<WistDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the captured expected-failure exception only when developer diagnostic exposure was requested.
    /// Internal and infrastructure faults are not converted into TryCompile results.
    /// </summary>
    public Exception? Exception { get; }

    public WistOptimizationReport OptimizationReport { get; }

    internal static WistCompileResult<TDelegate> Success(WistProgram<TDelegate> program) =>
        new(program, null, Array.Empty<WistDiagnostic>(), null, program.Metadata.OptimizationReport);

    internal static WistCompileResult<TDelegate> Failure(
        WistFailureKind failureKind,
        Exception? exception,
        IReadOnlyList<WistDiagnostic> diagnostics,
        WistOptimizationReport optimizationReport) =>
        new(null, failureKind, diagnostics, exception, optimizationReport);
}
