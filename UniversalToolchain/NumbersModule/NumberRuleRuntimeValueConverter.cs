using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace NumbersModule;

public sealed class NumberRuleRuntimeValueConverter : IRuleRuntimeValueConverter
{
    public bool TryConvert(
        object? value,
        out object? runtimeValue,
        out ToolchainDiagnostic? diagnostic,
        string argumentName,
        string ruleName,
        RuleTypeDescriptor expectedType)
    {
        argumentName = argumentName.ArgNotNull();
        ruleName = ruleName.ArgNotNull();
        expectedType = expectedType.ArgNotNull();

        runtimeValue = null;
        diagnostic = null;

        if (value == null)
        {
            diagnostic = CreateDiagnostic(
                ToolchainDiagnosticCodes.RuleArgumentNull,
                $"Argument '{argumentName}' for rule '{ruleName}' must not be null.");
            return false;
        }

        if (value is RealNumberImpl realNumber)
        {
            runtimeValue = realNumber;
            return true;
        }

        if (TryConvertToDouble(value, out var doubleValue))
        {
            runtimeValue = new RealNumberImpl(doubleValue);
            return true;
        }

        diagnostic = CreateDiagnostic(
            ToolchainDiagnosticCodes.RuleArgumentTypeMismatch,
            $"Argument '{argumentName}' for rule '{ruleName}' must have type '{expectedType.Name}'. Actual runtime type: '{value.GetType().FullName}'.");
        return false;
    }

    private static bool TryConvertToDouble(object value, out double result)
    {
        switch (value)
        {
            case double doubleValue:
                result = doubleValue;
                return true;
            case float floatValue:
                result = floatValue;
                return true;
            case decimal decimalValue:
                result = (double)decimalValue;
                return true;
            case int intValue:
                result = intValue;
                return true;
            case long longValue:
                result = longValue;
                return true;
            case short shortValue:
                result = shortValue;
                return true;
            case byte byteValue:
                result = byteValue;
                return true;
            default:
                result = default;
                return false;
        }
    }

    private static ToolchainDiagnostic CreateDiagnostic(string code, string message)
    {
        return new ToolchainDiagnostic(
            code,
            ToolchainDiagnosticSeverity.Error,
            message,
            null,
            []);
    }
}
