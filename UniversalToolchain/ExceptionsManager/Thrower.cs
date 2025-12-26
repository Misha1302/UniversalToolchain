using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ExceptionsManager;

public static class Thrower
{
    [DoesNotReturn]
    public static void InvalidOpEx(string message = "")
    {
        throw new InvalidOperationException(message);
    }

    [DoesNotReturn]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static void AssertationFail(string errorMessage = "")
    {
        InvalidOpEx($"Assertion failed: {errorMessage}");
    }

    [DoesNotReturn]
    public static T InvalidOpEx<T>(string message = "")
    {
        throw new InvalidOperationException(message);
    }

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
            AssertationFail(errorMessage == "" ? expression : errorMessage);
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
        throw new NotImplementedException(msg);
    }

    [DoesNotReturn]
    public static T NullException<T>(string errorMessage = "")
    {
        throw new NullReferenceException(errorMessage);
    }
}