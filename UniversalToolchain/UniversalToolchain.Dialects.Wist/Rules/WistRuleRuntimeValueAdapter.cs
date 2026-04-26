using NumbersModule.Core;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public sealed class WistRuleRuntimeValueAdapter
{
    public bool TryConvert(
        RuleTypeDescriptor type,
        object? value,
        out object? runtimeValue,
        out ToolchainDiagnostic? diagnostic,
        string argumentName,
        string ruleName)
    {
        type = type.ArgNotNull();
        argumentName = argumentName.ArgNotNull();
        ruleName = ruleName.ArgNotNull();

        runtimeValue = null;
        diagnostic = null;

        if (value == null)
        {
            diagnostic = CreateDiagnostic(
                ToolchainDiagnosticCodes.RuleArgumentNull,
                $"Argument '{argumentName}' for rule '{ruleName}' must not be null.");
            return false;
        }

        if (type.Name == "number")
            return TryConvertNumber(value, out runtimeValue, out diagnostic, argumentName, ruleName);

        if (type.Name == "bool")
        {
            if (value is bool)
            {
                runtimeValue = value;
                return true;
            }

            diagnostic = CreateTypeMismatch(type, value, argumentName, ruleName);
            return false;
        }

        diagnostic = CreateTypeMismatch(type, value, argumentName, ruleName);
        return false;
    }

    private static bool TryConvertNumber(
        object value,
        out object runtimeValue,
        out ToolchainDiagnostic? diagnostic,
        string argumentName,
        string ruleName)
    {
        diagnostic = null;

        if (value is RealNumberImpl)
        {
            runtimeValue = value;
            return true;
        }

        if (TryConvertToDouble(value, out var doubleValue))
        {
            runtimeValue = new RealNumberImpl(doubleValue);
            return true;
        }

        runtimeValue = null!;
        diagnostic = CreateTypeMismatch(new RuleTypeDescriptor("number"), value, argumentName, ruleName);
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

    private static ToolchainDiagnostic CreateTypeMismatch(
        RuleTypeDescriptor type,
        object value,
        string argumentName,
        string ruleName)
    {
        return CreateDiagnostic(
            ToolchainDiagnosticCodes.RuleArgumentTypeMismatch,
            $"Argument '{argumentName}' for rule '{ruleName}' must have type '{type.Name}'. Actual runtime type: '{value.GetType().FullName}'.");
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
