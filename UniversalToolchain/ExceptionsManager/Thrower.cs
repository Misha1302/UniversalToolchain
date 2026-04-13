namespace ExceptionsManager;

public static class Thrower
{
    [DoesNotReturn]
    public static void InvalidOpEx(string message = "")
    {
        throw new InvalidOperationException(FormatMessage(message, "Operation is invalid in the current context."));
    }

    [DoesNotReturn]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static void AssertionFail(string errorMessage = "")
    {
        InvalidOpEx($"Assertion failed: {errorMessage}");
    }

    [DoesNotReturn]
    public static T InvalidOpEx<T>(string message = "") => throw new InvalidOperationException(FormatMessage(message, "Operation is invalid in the current context."));

    [DebuggerStepThrough]
    [DebuggerHidden]
    public static void AssertAlways(
        [DoesNotReturnIf(false)] bool cond,
        string errorMessage = "",
        [CallerArgumentExpression(nameof(cond))]
        string expression = ""
    )
    {
        if (!cond)
            AssertionFail(errorMessage == "" ? expression : errorMessage);
    }

    [return: NotNull]
    public static T NotNull<T>(this T? obj, string errorMessage = "")
    {
        if (obj == null)
            NullException<object>(errorMessage);
        return obj;
    }

    [DoesNotReturn]
    public static void NotImplementedException(string msg = "")
    {
        throw new NotImplementedException(FormatMessage(msg, "Feature is not implemented yet."));
    }

    [DoesNotReturn]
    public static T NullException<T>(string errorMessage = "") => throw new NullReferenceException(FormatMessage(errorMessage, "Encountered unexpected null reference."));

    [DoesNotReturn]
    public static void FileNotFound(string filePath, string message = "")
    {
        throw new FileNotFoundException(FormatMessage(message, $"File was not found: '{filePath}'."), filePath);
    }

    [DoesNotReturn]
    public static void ArgumentNull(string paramName, string message = "")
    {
        throw new ArgumentNullException(paramName, FormatMessage(message, $"Argument '{paramName}' cannot be null."));
    }

    [DoesNotReturn]
    public static void Argument(string paramName, string message)
    {
        throw new ArgumentException(FormatMessage(message, $"Invalid value for argument '{paramName}'."), paramName);
    }

    [DoesNotReturn]
    public static T ArgumentOutOfRange<T>(string paramName = "", string message = "") => throw new ArgumentOutOfRangeException(paramName, FormatMessage(message, "Argument value is out of range."));

    [DoesNotReturn]
    public static T NotSupported<T>(string message = "") => throw new NotSupportedException(FormatMessage(message, "Operation is not supported."));

    [DoesNotReturn]
    public static T InvalidCast<T>(string message = "") => throw new InvalidCastException(FormatMessage(message, "Value cannot be converted to target type."));

    private static string FormatMessage(string message, string fallback) => string.IsNullOrWhiteSpace(message) ? fallback : message;

    [DoesNotReturn]
    public static void MultipleDefinition(string message)
    {
        throw new InvalidOperationException($"There are multiple definitions of {message}");
    }
}