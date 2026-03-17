using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Default deterministic resolver for runtime composition.
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
        foreach (var backendName in buildPlan.EnabledBackends.OrderBy(x => x, StringComparer.Ordinal))
        {
            if (!TryParseBackendTarget(backendName, out var target))
            {
                diagnostics.Add(new DialectDiagnostic(
                    "R005",
                    $"Build plan contains unsupported backend token '{backendName}'.",
                    DialectDiagnosticSeverity.Error));
                continue;
            }

            if (!registry.Backends.TryGetValue(target, out var backendDescriptor))
            {
                diagnostics.Add(new DialectDiagnostic(
                    "R002",
                    $"Runtime backend descriptor '{backendName}' was not registered.",
                    DialectDiagnosticSeverity.Error));
                continue;
            }

            enabledBackends.Add(backendDescriptor);
        }

        var enabledOptimizers = new List<RuntimeOptimizerDescriptor>();
        foreach (var optimizer in buildPlan.OptimizerDirectives.Where(x => x.Enabled).OrderBy(x => x.Name, StringComparer.Ordinal).ThenBy(x => x.Target))
        {
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

    private static bool TryParseBackendTarget(string backendName, out DialectBackendTarget target)
    {
        return DialectBackendTargetText.TryParse(backendName, allowAny: false, out target);
    }
}
