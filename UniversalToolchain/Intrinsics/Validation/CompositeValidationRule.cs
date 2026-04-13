using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Validation;

/// <summary>
/// Runs multiple validation rules in a deterministic sequence.
/// </summary>
public sealed class CompositeValidationRule : IIntrinsicValidationRule
{
    private readonly IReadOnlyList<IIntrinsicValidationRule> _rules;

    public CompositeValidationRule(params IIntrinsicValidationRule[] rules)
    {
        if (rules == null)
            Thrower.ArgumentNull(nameof(rules));

        _rules = rules;
    }

    public void Validate(IntrinsicInvocation invocation, IIntrinsicTypeResolutionContext context)
    {
        foreach (var rule in _rules)
        {
            rule.Validate(invocation, context);
        }
    }
}
