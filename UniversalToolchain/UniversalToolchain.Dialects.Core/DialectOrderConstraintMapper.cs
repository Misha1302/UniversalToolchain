using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Core;

internal static class DialectOrderConstraintMapper
{
    public static List<DialectOrderConstraint> FromSyntaxRules(IReadOnlyList<OrderRule> rules)
    {
        return rules.Select(ToOrderConstraint).ToList();
    }

    public static List<DialectOrderConstraint> FromCompiledDirectives(IReadOnlyList<DialectOrderDirective> directives)
    {
        return directives.Select(ToOrderConstraint).ToList();
    }

    private static DialectOrderConstraint ToOrderConstraint(OrderRule rule)
    {
        return new DialectOrderConstraint(
            ToConstraintKind(rule.Kind),
            rule.ModuleName,
            rule.RelatedModuleName);
    }

    private static DialectOrderConstraint ToOrderConstraint(DialectOrderDirective directive)
    {
        return new DialectOrderConstraint(
            ToConstraintKind(directive.Kind),
            directive.SourceModule,
            directive.TargetModule);
    }

    private static DialectOrderConstraintKind ToConstraintKind(OrderRuleKind kind)
    {
        return kind switch
        {
            OrderRuleKind.Before => DialectOrderConstraintKind.Before,
            OrderRuleKind.After => DialectOrderConstraintKind.After,
            _ => DialectOrderConstraintKind.Requires,
        };
    }

    private static DialectOrderConstraintKind ToConstraintKind(DialectOrderDirectiveKind kind)
    {
        return kind switch
        {
            DialectOrderDirectiveKind.Before => DialectOrderConstraintKind.Before,
            DialectOrderDirectiveKind.After => DialectOrderConstraintKind.After,
            _ => DialectOrderConstraintKind.Requires,
        };
    }
}
