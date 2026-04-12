using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Core;

internal static class DialectOrderConstraintMapper
{
    public static List<DialectOrderConstraint> FromSyntaxRules(IReadOnlyList<OrderRule> rules) => rules.Select(ToOrderConstraint).ToList();

    public static List<DialectOrderConstraint> FromCompiledDirectives(IReadOnlyList<DialectOrderDirective> directives) => directives.Select(ToOrderConstraint).ToList();

    public static List<DialectOrderConstraint> FromBindingRules(IReadOnlyList<OrderBindingDirectiveRecord> rules) => rules.Select(ToOrderConstraint).ToList();

    public static List<DialectOrderConstraint> FromDefinitionRules(IReadOnlyList<OrderRule> rules) => rules.Select(ToOrderConstraint).ToList();

    public static List<OrderRule> ToDefinitionRules(IReadOnlyList<DialectOrderConstraint> constraints) => constraints.Select(ToDefinitionRule).ToList();

    private static DialectOrderConstraint ToOrderConstraint(OrderRule rule) =>
        new(
            ToConstraintKind(rule.Kind),
            rule.ModuleName,
            rule.RelatedModuleName);

    private static DialectOrderConstraint ToOrderConstraint(DialectOrderDirective directive) =>
        new(
            ToConstraintKind(directive.Kind),
            directive.SourceModule,
            directive.TargetModule);

    private static DialectOrderConstraint ToOrderConstraint(OrderBindingDirectiveRecord rule) =>
        new(
            ToConstraintKind(rule.Kind),
            rule.ModuleName,
            rule.RelatedModuleName);

    private static OrderRule ToDefinitionRule(DialectOrderConstraint constraint) => new(ToDefinitionKind(constraint.Kind), constraint.SourceModule, constraint.TargetModule);

    private static DialectOrderConstraintKind ToConstraintKind(OrderRuleKind kind)
    {
        return kind switch
        {
            OrderRuleKind.Before => DialectOrderConstraintKind.Before,
            OrderRuleKind.After => DialectOrderConstraintKind.After,
            _ => DialectOrderConstraintKind.Requires
        };
    }

    private static DialectOrderConstraintKind ToConstraintKind(DialectOrderDirectiveKind kind)
    {
        return kind switch
        {
            DialectOrderDirectiveKind.Before => DialectOrderConstraintKind.Before,
            DialectOrderDirectiveKind.After => DialectOrderConstraintKind.After,
            _ => DialectOrderConstraintKind.Requires
        };
    }

    private static OrderRuleKind ToDefinitionKind(DialectOrderConstraintKind kind)
    {
        return kind switch
        {
            DialectOrderConstraintKind.Before => OrderRuleKind.Before,
            DialectOrderConstraintKind.After => OrderRuleKind.After,
            _ => OrderRuleKind.Requires
        };
    }
}
