namespace UniversalToolchain.Wist;

/// <summary>
///     Represents a non-throwing typed Wist compilation result.
/// </summary>
public sealed class WistCompileResult<TDelegate>
    where TDelegate : Delegate
{
    private WistCompileResult(
        WistProgram<TDelegate>? program,
        IReadOnlyList<WistDiagnostic> diagnostics,
        Exception? exception,
        WistOptimizationReport optimizationReport)
    {
        Program = program;
        Diagnostics = diagnostics;
        Exception = exception;
        OptimizationReport = optimizationReport ?? throw new ArgumentNullException(nameof(optimizationReport));
    }

    /// <summary>
    ///     Gets whether compilation succeeded.
    /// </summary>
    public bool IsSuccess => Program != null;

    /// <summary>
    ///     Gets the compiled program when compilation succeeded.
    /// </summary>
    public WistProgram<TDelegate>? Program { get; }

    /// <summary>
    ///     Gets stable structured diagnostics produced by compilation.
    /// </summary>
    public IReadOnlyList<WistDiagnostic> Diagnostics { get; }

    /// <summary>
    ///     Gets the captured exception for unexpected-fault investigation.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the observed optimization-route report, including reports captured before a failure.
    /// </summary>
    public WistOptimizationReport OptimizationReport { get; }

    /// <summary>
    ///     Gets the first error message when compilation failed.
    /// </summary>
    public string? Message => Diagnostics
        .FirstOrDefault(static diagnostic => diagnostic.Severity == WistDiagnosticSeverity.Error)
        ?.Message;

    internal static WistCompileResult<TDelegate> Success(WistProgram<TDelegate> program) =>
        new(program, Array.Empty<WistDiagnostic>(), null, program.Metadata.OptimizationReport);

    internal static WistCompileResult<TDelegate> Failure(
        Exception exception,
        IReadOnlyList<WistDiagnostic> diagnostics,
        WistOptimizationReport optimizationReport) =>
        new(null, diagnostics, exception, optimizationReport);
}
