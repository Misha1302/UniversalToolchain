using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace ConditionsModule;

public sealed class BooleanRuleRuntimeValueConverter : IRuleRuntimeValueConverter
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

        if (value is bool booleanValue)
        {
            runtimeValue = booleanValue;
            return true;
        }

        diagnostic = CreateDiagnostic(
            ToolchainDiagnosticCodes.RuleArgumentTypeMismatch,
            $"Argument '{argumentName}' for rule '{ruleName}' must have type '{expectedType.Name}'. Actual runtime type: '{value.GetType().FullName}'.");
        return false;
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
