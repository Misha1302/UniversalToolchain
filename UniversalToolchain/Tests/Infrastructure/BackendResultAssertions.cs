using ExceptionsManager;
using NumbersModule.Core;

namespace Tests.Infrastructure;

public static class BackendResultAssertions
{
    public static void AssertEquivalent(object? left, object? right)
    {
        if (left is null || right is null)
        {
            Assert.That(left, Is.EqualTo(right));
            return;
        }

        if (left is bool || right is bool)
        {
            Assert.That(AsBool(left), Is.EqualTo(AsBool(right)));
            return;
        }

        Assert.That(AsNumber(left), Is.EqualTo(AsNumber(right)).Within(1e-9));
    }

    public static double AsNumber(object? value)
    {
        return value switch
        {
            int intValue => intValue,
            long longValue => longValue,
            float floatValue => floatValue,
            double doubleValue => doubleValue,
            decimal decimalValue => (double)decimalValue,
            RealNumberImpl realNumberImpl => realNumberImpl.GetValue(),
            _ => Thrower.InvalidOpEx<double>(
                $"Value '{value?.ToString() ?? "null"}' of runtime type '{value?.GetType().FullName ?? "null"}' is not a supported numeric result.")
        };
    }

    public static bool AsBool(object? value)
    {
        return value switch
        {
            bool boolValue => boolValue,
            _ => Thrower.InvalidOpEx<bool>(
                $"Value '{value?.ToString() ?? "null"}' of runtime type '{value?.GetType().FullName ?? "null"}' is not a boolean result.")
        };
    }
}