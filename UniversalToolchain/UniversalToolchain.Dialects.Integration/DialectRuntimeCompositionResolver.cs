using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Default deterministic resolver for runtime composition.
/// </summary>
public sealed class DialectRuntimeCompositionResolver : IDialectRuntimeCompositionResolver
{
    public DialectRuntimeComposition Resolve(DialectBuildPlan buildPlan, DialectRuntimeDescriptorRegistry registry)
    {
        if (buildPlan == null)
            Thrower.ArgumentNull(nameof(buildPlan));

        if (registry == null)
            Thrower.ArgumentNull(nameof(registry));

        var diagnostics = new List<DialectDiagnostic>();
        diagnostics.AddRange(buildPlan.ValidationResult.Diagnostics);

        var orderedModules = new List<RuntimeModuleDescriptor>();
        foreach (var moduleName in buildPlan.OrderedModules)
        {
            if (!registry.Modules.TryGetValue(moduleName, out var descriptor))
            {
                diagnostics.Add(new DialectDiagnostic(
                    "R001",
                    $"Runtime module descriptor '{moduleName}' was not registered.",
                    DialectDiagnosticSeverity.Error));
                continue;
            }

            orderedModules.Add(descriptor);
        }

        var enabledBackends = new List<RuntimeBackendDescriptor>();
        var enabledBackendTargets = new HashSet<DialectBackendTarget>();
        foreach (var backendTarget in buildPlan.EnabledBackends.OrderBy(DialectBackendTargetText.ToText, StringComparer.Ordinal))
        {
            enabledBackendTargets.Add(backendTarget);

            if (!registry.Backends.TryGetValue(backendTarget, out var backendDescriptor))
            {
                diagnostics.Add(new DialectDiagnostic(
                    "R002",
                    $"Runtime backend descriptor '{DialectBackendTargetText.ToText(backendTarget)}' was not registered.",
                    DialectDiagnosticSeverity.Error));
                continue;
            }

            enabledBackends.Add(backendDescriptor);
        }

        var enabledOptimizers = new List<RuntimeOptimizerDescriptor>();
        foreach (var optimizer in buildPlan.OptimizerDirectives.Where(x => x.Enabled).OrderBy(x => x.Name, StringComparer.Ordinal).ThenBy(x => x.Target))
        {
            if (!IsDirectiveTargetEnabled(optimizer.Target, enabledBackendTargets))
                continue;

            if (!registry.Optimizers.TryGetValue(optimizer.Name, out var descriptor))
            {
                diagnostics.Add(new DialectDiagnostic(
                    "R003",
                    $"Runtime optimizer descriptor '{optimizer.Name}' was not registered.",
                    DialectDiagnosticSeverity.Error));
                continue;
            }

            enabledOptimizers.Add(descriptor);
        }

        var allowedIntrinsics = new List<RuntimeIntrinsicDescriptor>();
        foreach (var intrinsic in buildPlan.IntrinsicDirectives.Where(x => x.Allowed).OrderBy(x => x.Name, StringComparer.Ordinal).ThenBy(x => x.Target))
        {
            if (!IsDirectiveTargetEnabled(intrinsic.Target, enabledBackendTargets))
                continue;

            if (!TryResolveIntrinsic(registry, intrinsic.Name, intrinsic.Target, out var resolved))
            {
                diagnostics.Add(new DialectDiagnostic(
                    "R004",
                    $"Runtime intrinsic descriptor '{intrinsic.Name}' for '{DialectBackendTargetText.ToText(intrinsic.Target)}' was not registered.",
                    DialectDiagnosticSeverity.Error));
                continue;
            }

            allowedIntrinsics.Add(resolved);
        }

        var validation = new DialectValidationResult(diagnostics);

        return new DialectRuntimeComposition(
            buildPlan.Name,
            orderedModules,
            enabledBackends,
            enabledOptimizers,
            allowedIntrinsics,
            validation);
    }

    private static bool IsDirectiveTargetEnabled(
        DialectBackendTarget target,
        IReadOnlySet<DialectBackendTarget> enabledBackendTargets)
    {
        if (target == DialectBackendTarget.Any)
            return enabledBackendTargets.Count > 0;

        return enabledBackendTargets.Contains(target);
    }

    private static bool TryResolveIntrinsic(
        DialectRuntimeDescriptorRegistry registry,
        string name,
        DialectBackendTarget target,
        out RuntimeIntrinsicDescriptor descriptor)
    {
        if (registry.Intrinsics.TryGetValue((name, target), out descriptor!))
            return true;

        if (target != DialectBackendTarget.Any && registry.Intrinsics.TryGetValue((name, DialectBackendTarget.Any), out descriptor!))
            return true;

        descriptor = null!;
        return false;
    }
}