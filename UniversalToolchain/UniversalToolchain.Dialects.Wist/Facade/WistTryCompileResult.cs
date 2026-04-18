using BasicCore.Compilation;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist.Facade;

/// <summary>
///     Captures the outcome of a Wist facade compilation attempt.
/// </summary>
public sealed class WistTryCompileResult
{
    private WistTryCompileResult(bool isSuccess, ICompiledArtifact? artifact, Exception? exception)
    {
        IsSuccess = isSuccess;
        Artifact = artifact;
        Exception = exception;
    }

    /// <summary>
    ///     Gets a value indicating whether compilation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Gets the compiled artifact when compilation succeeds.
    /// </summary>
    public ICompiledArtifact? Artifact { get; }

    /// <summary>
    ///     Gets the exception captured when compilation fails.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    ///     Gets a human-readable failure message, or null when compilation succeeds.
    /// </summary>
    public string? ErrorMessage => Exception?.Message;

    /// <summary>
    ///     Creates a successful compilation result.
    /// </summary>
    public static WistTryCompileResult Success(ICompiledArtifact artifact)
    {
        artifact = artifact.ArgNotNull();

        return new WistTryCompileResult(true, artifact, null);
    }

    /// <summary>
    ///     Creates a failed compilation result.
    /// </summary>
    public static WistTryCompileResult Failure(Exception exception)
    {
        exception = exception.ArgNotNull();

        return new WistTryCompileResult(false, null, exception);
    }
}