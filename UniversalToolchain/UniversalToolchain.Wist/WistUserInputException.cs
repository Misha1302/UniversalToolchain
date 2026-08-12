namespace UniversalToolchain.Wist;

/// <summary>
/// Identifies facade-owned argument validation failures that are safe to represent as user input.
/// Deriving from ArgumentException preserves the existing caller-visible exception family without
/// treating arbitrary framework ArgumentException instances as invalid formulas.
/// </summary>
internal sealed class WistUserInputException : ArgumentException
{
    public WistUserInputException(string message, string? paramName = null)
        : base(message, paramName)
    {
    }
}
