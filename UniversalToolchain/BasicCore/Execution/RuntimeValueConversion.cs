using System.Globalization;

namespace BasicCore.Execution;

public enum RuntimeValueConversionFailureKind
{
    InvalidFormat,
    Overflow,
    UnsupportedConversion,
    NullabilityViolation,
    PrecisionLoss
}

public sealed class RuntimeValueConversionException : InvalidOperationException
{
    public RuntimeValueConversionException(
        RuntimeValueConversionFailureKind failureKind,
        Type? sourceType,
        Type targetType,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        SourceType = sourceType;
        TargetType = targetType;
    }

    public RuntimeValueConversionFailureKind FailureKind { get; }
    public Type? SourceType { get; }
    public Type TargetType { get; }
}

public interface IRuntimeValueConversionService
{
    object? Convert(object? value, Type targetType);
}


public static class RuntimeValueConversion
{
    public static object? Convert(object? value, Type targetType) =>
        RuntimeValueConversionService.Default.Convert(value, targetType);
}

public sealed class RuntimeValueConversionService : IRuntimeValueConversionService
{
    public static RuntimeValueConversionService Default { get; } = new();

    public object? Convert(object? value, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        var nullableTarget = Nullable.GetUnderlyingType(targetType);
        var effectiveTarget = nullableTarget ?? targetType;

        if (value == null)
        {
            if (!targetType.IsValueType || nullableTarget != null)
                return null;
            throw Failure(
                RuntimeValueConversionFailureKind.NullabilityViolation,
                null,
                targetType,
                $"Null cannot be converted to non-nullable type '{targetType.FullName}'.");
        }

        var sourceType = value.GetType();
        if (targetType.IsInstanceOfType(value) || effectiveTarget.IsInstanceOfType(value))
            return value;

        try
        {
            if (effectiveTarget.IsEnum)
                return ConvertEnum(value, sourceType, effectiveTarget);

            if (WouldLosePrecision(value, sourceType, effectiveTarget))
            {
                throw Failure(
                    RuntimeValueConversionFailureKind.PrecisionLoss,
                    sourceType,
                    targetType,
                    $"Conversion from '{sourceType.FullName}' to '{targetType.FullName}' would lose precision.");
            }

            return System.Convert.ChangeType(value, effectiveTarget, CultureInfo.InvariantCulture);
        }
        catch (RuntimeValueConversionException)
        {
            throw;
        }
        catch (FormatException exception)
        {
            throw Failure(
                RuntimeValueConversionFailureKind.InvalidFormat,
                sourceType,
                targetType,
                $"Value '{value}' is not in a valid invariant format for '{targetType.FullName}'.",
                exception);
        }
        catch (OverflowException exception)
        {
            throw Failure(
                RuntimeValueConversionFailureKind.Overflow,
                sourceType,
                targetType,
                $"Value '{value}' is outside the range of '{targetType.FullName}'.",
                exception);
        }
        catch (Exception exception) when (exception is InvalidCastException or ArgumentException)
        {
            throw Failure(
                RuntimeValueConversionFailureKind.UnsupportedConversion,
                sourceType,
                targetType,
                $"Conversion from '{sourceType.FullName}' to '{targetType.FullName}' is not supported.",
                exception);
        }
    }

    public T Convert<T>(object? value) => (T)Convert(value, typeof(T))!;

    private static object ConvertEnum(object value, Type sourceType, Type targetType)
    {
        if (value is string text)
        {
            if (Enum.TryParse(targetType, text, ignoreCase: false, out var parsed))
                return parsed!;
            throw Failure(
                RuntimeValueConversionFailureKind.InvalidFormat,
                sourceType,
                targetType,
                $"Value '{text}' is not a declared name or numeric value of enum '{targetType.FullName}'.");
        }

        var underlying = Enum.GetUnderlyingType(targetType);
        var converted = System.Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
        return Enum.ToObject(targetType, converted!);
    }

    private static bool WouldLosePrecision(object value, Type sourceType, Type targetType)
    {
        if (!IsNumeric(sourceType) || !IsNumeric(targetType))
            return false;

        if (IsIntegral(targetType) && sourceType is not null && IsFractional(sourceType))
        {
            var decimalValue = System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            if (decimal.Truncate(decimalValue) != decimalValue)
                return true;
        }

        if (targetType == typeof(float))
        {
            var asDouble = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            var narrowed = (float)asDouble;
            return !double.IsNaN(asDouble) && (double)narrowed != asDouble;
        }

        if (targetType == typeof(double) && sourceType == typeof(decimal))
        {
            var source = (decimal)value;
            var narrowed = (double)source;
            try
            {
                return (decimal)narrowed != source;
            }
            catch (OverflowException)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNumeric(Type type) => Type.GetTypeCode(type) is
        TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or
        TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal;

    private static bool IsIntegral(Type type) => Type.GetTypeCode(type) is
        TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or
        TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64;

    private static bool IsFractional(Type type) => Type.GetTypeCode(type) is
        TypeCode.Single or TypeCode.Double or TypeCode.Decimal;

    private static RuntimeValueConversionException Failure(
        RuntimeValueConversionFailureKind kind,
        Type? sourceType,
        Type targetType,
        string message,
        Exception? inner = null) => new(kind, sourceType, targetType, message, inner);
}
