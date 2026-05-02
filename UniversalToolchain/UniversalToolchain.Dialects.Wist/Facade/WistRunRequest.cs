using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist.Facade;

/// <summary>
///     Describes one Wist facade execution request.
/// </summary>
public sealed class WistRunRequest
{
    public WistRunRequest(
        string code,
        IReadOnlyDictionary<string, object?> arguments,
        string backend = "compiler")
    {
        code = code.ArgNotNull();
        arguments = arguments.ArgNotNull();

        if (string.IsNullOrWhiteSpace(backend))
            Thrower.Argument(nameof(backend), "Backend name must not be empty.");

        Code = code;
        Arguments = arguments;
        Backend = backend;
    }

    /// <summary>
    ///     Gets Wist source text to execute.
    /// </summary>
    public string Code { get; }

    /// <summary>
    ///     Gets named runtime argument values.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Arguments { get; }

    /// <summary>
    ///     Gets the backend id or alias selected for execution.
    /// </summary>
    public string Backend { get; }
}