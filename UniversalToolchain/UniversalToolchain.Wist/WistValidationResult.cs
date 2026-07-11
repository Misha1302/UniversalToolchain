namespace UniversalToolchain.Wist;

/// <summary>
///     Represents a public validation result for Wist source text.
/// </summary>
public sealed class WistValidationResult
{
    private WistValidationResult(
        bool isValid,
        IReadOnlyList<WistDiagnostic> diagnostics,
        Exception? exception,
        WistOptimizationReport optimizationReport)
    {
        IsValid = isValid;
        Diagnostics = diagnostics;
        Exception = exception;
        OptimizationReport = optimizationReport ?? throw new ArgumentNullException(nameof(optimizationReport));
    }

    /// <summary>
    ///     Gets a value indicating whether validation succeeded.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    ///     Gets stable structured diagnostics produced by validation.
    /// </summary>
    public IReadOnlyList<WistDiagnostic> Diagnostics { get; }

    /// <summary>
    ///     Gets the first error message for compatibility with the preview.2 facade.
    /// </summary>
    public string? Message => Diagnostics
        .FirstOrDefault(static diagnostic => diagnostic.Severity == WistDiagnosticSeverity.Error)
        ?.Message;

    /// <summary>
    ///     Gets the captured exception for unexpected-fault investigation.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the observed optimization-route report, including reports captured before a failure.
    /// </summary>
    public WistOptimizationReport OptimizationReport { get; }

    internal static WistValidationResult Success(WistOptimizationReport optimizationReport) =>
        new(true, Array.Empty<WistDiagnostic>(), null, optimizationReport);

    internal static WistValidationResult Failure(
        Exception exception,
        IReadOnlyList<WistDiagnostic> diagnostics,
        WistOptimizationReport optimizationReport) =>
        new(false, diagnostics, exception, optimizationReport);
}
