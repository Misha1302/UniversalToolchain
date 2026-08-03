using System.Reflection;

namespace Tests.Infrastructure;

internal static class BackendValueNormalizer
{
    public static object? Normalize(object? value)
    {
        if (value is null)
            return null;

        if (value is int intValue)
            return intValue;

        if (value is long longValue)
            return longValue;

        if (value is float floatValue)
            return (double)floatValue;

        if (value is double doubleValue)
            return doubleValue;

        if (value is decimal decimalValue)
            return (double)decimalValue;

        if (value is bool boolValue)
            return boolValue;

        var getValue = value.GetType().GetMethod(
            "GetValue",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);
        if (getValue?.Invoke(value, null) is { } wrappedValue &&
            !ReferenceEquals(wrappedValue, value))
        {
            return Normalize(wrappedValue);
        }

        return value;
    }

    public static T ConvertTo<T>(object? value)
    {
        var normalized = Normalize(value);

        if (normalized is T typed)
            return typed;

        if (normalized is IConvertible convertible)
            return (T)Convert.ChangeType(convertible, typeof(T));

        return Thrower.InvalidOpEx<T>(
            $"Cannot convert normalized backend value from {normalized?.GetType().FullName ?? "<null>"} to {typeof(T).FullName}.");
    }
}