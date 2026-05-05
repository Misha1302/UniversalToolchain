namespace UniversalToolchain.Wist;

/// <summary>
///     Represents a public validation result for Wist source text.
/// </summary>
public sealed class WistValidationResult
{
    private WistValidationResult(bool isValid, string? message, Exception? exception)
    {
        IsValid = isValid;
        Message = message;
        Exception = exception;
    }

    /// <summary>
    ///     Gets a value indicating whether validation succeeded.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    ///     Gets a human-readable validation message.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    ///     Gets the captured exception for advanced diagnostics.
    /// </summary>
    public Exception? Exception { get; }

    internal static WistValidationResult Success() => new(true, null, null);

    internal static WistValidationResult Failure(Exception exception) => new(false, exception.Message, exception);
}