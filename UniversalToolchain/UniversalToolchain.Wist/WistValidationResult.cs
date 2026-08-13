namespace UniversalToolchain.Wist;

/// <summary>
///     Represents a public validation result for Wist source text.
/// </summary>
public sealed class WistValidationResult
{
    private WistValidationResult(
        bool isValid,
        WistFailureKind? failureKind,
        IReadOnlyList<WistDiagnostic> diagnostics,
        Exception? exception,
        WistOptimizationReport optimizationReport)
    {
        IsValid = isValid;
        FailureKind = failureKind;
        Diagnostics = diagnostics;
        Exception = exception;
        OptimizationReport = optimizationReport ?? throw new ArgumentNullException(nameof(optimizationReport));
    }

    /// <summary>Gets a value indicating whether validation succeeded.</summary>
    public bool IsValid { get; }

    /// <summary>Gets the expected failure category, or null for success.</summary>
    public WistFailureKind? FailureKind { get; }

    /// <summary>Gets stable structured diagnostics produced by validation.</summary>
    public IReadOnlyList<WistDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the captured expected-failure exception only when developer diagnostic exposure was requested.
    /// Internal and infrastructure faults are not converted into validation results.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>Gets the observed optimization-route report, including reports captured before a failure.</summary>
    public WistOptimizationReport OptimizationReport { get; }

    internal static WistValidationResult Success(WistOptimizationReport optimizationReport) =>
        new(true, null, Array.Empty<WistDiagnostic>(), null, optimizationReport);

    internal static WistValidationResult Failure(
        WistFailureKind failureKind,
        Exception? exception,
        IReadOnlyList<WistDiagnostic> diagnostics,
        WistOptimizationReport optimizationReport) =>
        new(false, failureKind, diagnostics, exception, optimizationReport);
}
