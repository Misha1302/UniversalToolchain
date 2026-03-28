using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core;

internal static class DialectDefinitionBuildPlanProjector
{
    public static DialectBuildPlan Project(DialectDefinition definition, List<DialectDiagnostic> diagnostics, string cycleCode, string cycleMessagePrefix, string? missingReferenceCode = null, string? missingReferenceMessagePrefix = null)
    {
        if (definition == null)
            Thrower.ArgumentNull(nameof(definition));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        var securityProfile = definition.SecurityPolicy?.Profile;
        var capabilities = definition.CapabilityPolicy.Capabilities
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .ToList();
        var capabilityMap = capabilities.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);

        DialectSecurityCapabilityPolicyValidator.Validate(
            securityProfile,
            capabilityMap,
            diagnostics);

        var orderedModules = DialectSemanticNormalization.ResolveOrder(
            definition.ModulePolicy.IncludedModules,
            DialectOrderConstraintMapper.FromDefinitionRules(definition.OrderRules),
            diagnostics,
            cycleCode,
            cycleMessagePrefix,
            missingReferenceCode,
            missingReferenceMessagePrefix);

        var validationResult = new DialectValidationResult(diagnostics);

        return new DialectBuildPlan(
            definition.Name,
            definition.Version,
            orderedModules,
            ExpandBackendPolicy(definition.BackendPolicy.EnabledBackends),
            ExpandBackendPolicy(definition.BackendPolicy.DisabledBackends),
            ExpandIntrinsicPolicy(definition.IntrinsicPolicy.AllowedIntrinsics, true)
                .Concat(ExpandIntrinsicPolicy(definition.IntrinsicPolicy.ForbiddenIntrinsics, false))
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ThenBy(x => x.Target)
                .ToList(),
            ExpandOptimizerPolicy(definition.OptimizerPolicy.EnabledOptimizers, true)
                .Concat(ExpandOptimizerPolicy(definition.OptimizerPolicy.DisabledOptimizers, false))
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ThenBy(x => x.Target)
                .ToList(),
            securityProfile,
            capabilities,
            validationResult);
    }

    private static IReadOnlyList<DialectBackendId> ExpandBackendPolicy(IReadOnlyList<DialectBackendId> backends)
    {
        return backends.OrderBy(x => x).ToList();
    }

    private static IReadOnlyList<IntrinsicBuildDirective> ExpandIntrinsicPolicy(IEnumerable<string> directives, bool allowed)
    {
        return directives.Select(x => ParseScopedName(x, allowed)).ToList();
    }

    private static IReadOnlyList<OptimizerBuildDirective> ExpandOptimizerPolicy(IEnumerable<string> directives, bool enabled)
    {
        return directives.Select(x => ParseScopedOptimizer(x, enabled)).ToList();
    }

    private static IntrinsicBuildDirective ParseScopedName(string value, bool allowed)
    {
        var (name, target) = ParseScopedTarget(value);
        return new IntrinsicBuildDirective(name, allowed, target);
    }

    private static OptimizerBuildDirective ParseScopedOptimizer(string value, bool enabled)
    {
        var (name, target) = ParseScopedTarget(value);
        return new OptimizerBuildDirective(name, enabled, target);
    }

    private static (string Name, DialectBackendSelector Target) ParseScopedTarget(string value)
    {
        var parts = value.Split('@', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
            return (value, DialectBackendSelector.Any);

        if (!DialectBackendSelectorText.TryParseSelector(parts[1], false, out var target))
            Thrower.Argument(nameof(value), $"Scoped backend target '{parts[1]}' is not supported.");

        return (parts[0], target);
    }
}