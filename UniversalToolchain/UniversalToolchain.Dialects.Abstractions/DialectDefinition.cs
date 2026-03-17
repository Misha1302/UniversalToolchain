using System.Collections.ObjectModel;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
/// Represents an immutable dialect definition source model for the v1 DSL surface.
/// </summary>
public sealed class DialectDefinition
{
    private readonly ReadOnlyCollection<OrderRule> _orderRules;

    /// <summary>
    /// Creates a new dialect definition instance.
    /// </summary>
    /// <param name="name">Unique dialect name.</param>
    /// <param name="modulePolicy">Module include/exclude policy.</param>
    /// <param name="backendPolicy">Backend enable/disable policy.</param>
    /// <param name="intrinsicPolicy">Intrinsic allow/forbid policy.</param>
    /// <param name="optimizerPolicy">Optimizer enable/disable policy.</param>
    /// <param name="securityPolicy">Security profile policy.</param>
    /// <param name="capabilityPolicy">Named boolean capability policy.</param>
    /// <param name="orderRules">Optional module order rules.</param>
    /// <param name="version">Optional dialect version text.</param>
    /// <param name="baseDialectName">Optional base dialect name.</param>
    public DialectDefinition(
        string name,
        ModulePolicy modulePolicy,
        BackendPolicy backendPolicy,
        IntrinsicPolicy intrinsicPolicy,
        OptimizerPolicy optimizerPolicy,
        SecurityPolicy securityPolicy,
        CapabilityPolicy capabilityPolicy,
        IEnumerable<OrderRule>? orderRules = null,
        string? version = null,
        string? baseDialectName = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Dialect name must not be empty.");

        if (modulePolicy == null)
            Thrower.ArgumentNull(nameof(modulePolicy));

        if (backendPolicy == null)
            Thrower.ArgumentNull(nameof(backendPolicy));

        if (intrinsicPolicy == null)
            Thrower.ArgumentNull(nameof(intrinsicPolicy));

        if (optimizerPolicy == null)
            Thrower.ArgumentNull(nameof(optimizerPolicy));

        if (securityPolicy == null)
            Thrower.ArgumentNull(nameof(securityPolicy));

        if (capabilityPolicy == null)
            Thrower.ArgumentNull(nameof(capabilityPolicy));

        if (version != null && string.IsNullOrWhiteSpace(version))
            Thrower.Argument(nameof(version), "Dialect version must be null or a non-empty value.");

        if (baseDialectName != null && string.IsNullOrWhiteSpace(baseDialectName))
            Thrower.Argument(nameof(baseDialectName), "Base dialect name must be null or a non-empty value.");

        Name = name;
        Version = version;
        BaseDialectName = baseDialectName;
        ModulePolicy = modulePolicy;
        BackendPolicy = backendPolicy;
        IntrinsicPolicy = intrinsicPolicy;
        OptimizerPolicy = optimizerPolicy;
        SecurityPolicy = securityPolicy;
        CapabilityPolicy = capabilityPolicy;

        var rulesSnapshot = MaterializeRules(orderRules);
        _orderRules = new ReadOnlyCollection<OrderRule>(rulesSnapshot);
    }

    public string Name { get; }

    public string? Version { get; }

    public string? BaseDialectName { get; }

    public ModulePolicy ModulePolicy { get; }

    public IReadOnlyList<OrderRule> OrderRules => _orderRules;

    public BackendPolicy BackendPolicy { get; }

    public IntrinsicPolicy IntrinsicPolicy { get; }

    public OptimizerPolicy OptimizerPolicy { get; }

    public SecurityPolicy SecurityPolicy { get; }

    public CapabilityPolicy CapabilityPolicy { get; }

    private static List<OrderRule> MaterializeRules(IEnumerable<OrderRule>? orderRules)
    {
        if (orderRules == null)
            return [];

        var rules = new List<OrderRule>();
        foreach (var rule in orderRules)
        {
            if (rule == null)
                Thrower.Argument(nameof(orderRules), "Order rules must not contain null entries.");

            rules.Add(rule);
        }

        return rules;
    }
}
