using ExceptionsManager;
using System.Globalization;

namespace UniversalToolchain.Wist;

internal static class WistResultConverter
{
    public static T ConvertTo<T>(object? value)
    {
        if (value is T typedValue)
            return typedValue;

        if (value == null)
        {
            if (default(T) == null)
                return default!;

            Thrower.InvalidCast($"Cannot convert null Wist result to '{typeof(T)}'.");
        }

        if (TryReadCustomNumericValue(value, out var numericValue))
            return (T)Convert.ChangeType(numericValue, typeof(T), CultureInfo.InvariantCulture);

        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }

    private static bool TryReadCustomNumericValue(object value, out double numericValue)
    {
        var method = value.GetType().GetMethod("GetValue", Type.EmptyTypes);
        if (method != null && method.ReturnType == typeof(double))
        {
            numericValue = (double)method.Invoke(value, null)!;
            return true;
        }

        numericValue = default;
        return false;
    }
}