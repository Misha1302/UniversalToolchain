namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Defines one module dependency or ordering relation.
/// </summary>
public sealed class OrderRule
{
    public OrderRule(OrderRuleKind kind, string moduleName, string relatedModuleName)
    {
        if (!Enum.IsDefined(kind))
            Thrower.Argument(nameof(kind), "Order rule kind is not defined.");

        if (string.IsNullOrWhiteSpace(moduleName))
            Thrower.Argument(nameof(moduleName), "Module name must not be empty.");

        if (string.IsNullOrWhiteSpace(relatedModuleName))
            Thrower.Argument(nameof(relatedModuleName), "Related module name must not be empty.");

        if (string.Equals(moduleName, relatedModuleName, StringComparison.Ordinal))
            Thrower.Argument(nameof(relatedModuleName), "Module relation must reference a different module.");

        Kind = kind;
        ModuleName = moduleName;
        RelatedModuleName = relatedModuleName;
    }

    public OrderRuleKind Kind { get; }

    public string ModuleName { get; }

    public string RelatedModuleName { get; }
}