using CommonExceptions;

namespace UniversalToolchain.Wist;

/// <summary>
///     Reports source that uses a language feature outside the selected Wist dialect preset.
/// </summary>
public sealed class WistDialectFeatureException : WistException
{
    public WistDialectFeatureException(string message, Exception inner)
        : base(message, inner)
    {
        Stage = "Dialect";
    }
}
