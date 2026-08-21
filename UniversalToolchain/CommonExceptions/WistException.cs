namespace CommonExceptions;

public class WistException : ToolchainException
{
    public WistException(string message) : base(message)
    {
    }

    public WistException(string message, Exception inner) : base(message, inner)
    {
    }
}
