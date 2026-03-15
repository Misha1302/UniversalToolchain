namespace CommonExceptions;

public class WistException : Exception
{
    public string? Stage { get; protected set; }
    public SourceLocation? Location { get; protected set; }

    public WistException(string message) : base(message)
    {
    }

    public WistException(string message, Exception inner) : base(message, inner)
    {
    }

    public override string ToString()
    {
        var stagePart = string.IsNullOrWhiteSpace(Stage) ? string.Empty : $"[{Stage}] ";
        var locationPart = string.Empty;

        if (Location is { } location)
        {
            locationPart = $" at line {location.Line} column {location.Column}";
            if (!string.IsNullOrWhiteSpace(location.File))
                locationPart += $" in {location.File}";
        }

        return $"{stagePart}{Message}{locationPart}";
    }
}
