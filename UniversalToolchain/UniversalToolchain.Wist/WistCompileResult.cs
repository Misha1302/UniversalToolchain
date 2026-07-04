namespace UniversalToolchain.Wist;

/// <summary>
///     Represents a non-throwing typed Wist compilation result.
/// </summary>
public sealed class WistCompileResult<TDelegate>
    where TDelegate : Delegate
{
    private WistCompileResult(WistProgram<TDelegate>? program, Exception? exception)
    {
        Program = program;
        Exception = exception;
    }

    /// <summary>
    ///     Gets whether compilation succeeded.
    /// </summary>
    public bool IsSuccess => Program != null;

    /// <summary>
    ///     Gets the compiled program when compilation succeeded.
    /// </summary>
    public WistProgram<TDelegate>? Program { get; }

    /// <summary>
    ///     Gets the captured exception when compilation failed.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    ///     Gets a human-readable failure message when compilation failed.
    /// </summary>
    public string? Message => Exception?.Message;

    internal static WistCompileResult<TDelegate> Success(WistProgram<TDelegate> program) => new(program, null);

    internal static WistCompileResult<TDelegate> Failure(Exception exception) => new(null, exception);
}
