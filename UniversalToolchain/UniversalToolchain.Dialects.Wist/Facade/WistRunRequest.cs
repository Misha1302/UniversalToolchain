using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Describes one Wist facade execution request.
/// </summary>
public sealed class WistRunRequest
{
    public WistRunRequest(
        string code,
        IReadOnlyDictionary<string, object?> arguments,
        string mode = "compiler")
    {
        code = code.ArgNotNull();

        arguments = arguments.ArgNotNull();

        if (string.IsNullOrWhiteSpace(mode))
            Thrower.Argument(nameof(mode), "Execution mode must not be empty.");

        Code = code;
        Arguments = arguments;
        Mode = mode;
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
    ///     Gets the backend mode name or alias.
    /// </summary>
    public string Mode { get; }
}