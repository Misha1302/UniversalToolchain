using ExceptionsManager;
using System.Globalization;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Wist;

internal static class WistResultConverter
{
    public static T ConvertTo<T>(object? value)
    {
        value = WistRuntimeValueNormalizer.Normalize(value);

        if (value is T typedValue)
            return typedValue;

        if (value == null)
        {
            if (default(T) == null)
                return default!;

            return Thrower.InvalidCast<T>($"Cannot convert null Wist result to '{typeof(T)}'.");
        }

        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }
}
