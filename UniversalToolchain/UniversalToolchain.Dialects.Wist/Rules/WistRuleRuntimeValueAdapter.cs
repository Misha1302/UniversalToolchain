using ExceptionsManager;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public sealed class WistRuleRuntimeValueAdapter
{
    private readonly WistRuleRuntimeTypeResolver _typeResolver;

    public WistRuleRuntimeValueAdapter(WistRuleRuntimeTypeResolver typeResolver)
    {
        _typeResolver = typeResolver.ArgNotNull();
    }

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

        if (!_typeResolver.TryGetBinding(type, out var binding))
        {
            runtimeValue = null;
            diagnostic = CreateDiagnostic(
                ToolchainDiagnosticCodes.RuleArgumentTypeMismatch,
                $"Unsupported rule type '{type.Name}'.");
            return false;
        }

        return binding.Converter.TryConvert(
            value,
            out runtimeValue,
            out diagnostic,
            argumentName,
            ruleName,
            type);
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
