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
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    public void Validate(IntrinsicInvocation invocation, IIntrinsicTypeResolutionContext context)
    {
        foreach (var rule in _rules)
        {
            rule.Validate(invocation, context);
        }
    }
}
