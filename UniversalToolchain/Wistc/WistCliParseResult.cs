namespace Wistc;

public sealed class WistCliParseResult
{
    private WistCliParseResult(object? options, IReadOnlyList<WistCliParseError> errors)
    {
        Options = options;
        Errors = errors;
    }

    public object? Options { get; }
    public IReadOnlyList<WistCliParseError> Errors { get; }
    public bool IsSuccess => Options is not null && Errors.Count == 0;

    public static WistCliParseResult Success(object options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new WistCliParseResult(options, []);
    }

    public static WistCliParseResult Failure(params WistCliParseError[] errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new WistCliParseResult(null, errors);
    }
}
