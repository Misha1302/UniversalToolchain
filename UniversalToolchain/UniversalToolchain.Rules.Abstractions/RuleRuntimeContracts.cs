using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.Rules.Abstractions;

public sealed record RuleRuntimeTypeBinding(
    RuleTypeDescriptor RuleType,
    Type RuntimeType,
    IRuleRuntimeValueConverter Converter);

public interface IRuleRuntimeValueConverter
{
    bool TryConvert(
        object? value,
        out object? runtimeValue,
        out ToolchainDiagnostic? diagnostic,
        string argumentName,
        string ruleName,
        RuleTypeDescriptor expectedType);
}

public interface IRuleRuntimeTypeBindingProvider
{
    IReadOnlyList<RuleRuntimeTypeBinding> GetRuleRuntimeTypeBindings();
}
