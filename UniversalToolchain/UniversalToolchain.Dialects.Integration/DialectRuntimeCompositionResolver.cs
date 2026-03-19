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
            if (!registry.TryResolveModule(moduleName, out var descriptor))
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
        var enabledBackendMap = new Dictionary<DialectBackendId, RuntimeBackendDescriptor>();
        foreach (var backendId in buildPlan.EnabledBackends.OrderBy(x => x))
        {
            if (!registry.TryResolveBackend(backendId, out var backendDescriptor))
            {
                diagnostics.Add(new DialectDiagnostic(
                    "R002",
                    $"Runtime backend descriptor '{DialectBackendSelectorText.ToText(backendId)}' was not registered.",
                    DialectDiagnosticSeverity.Error));
                continue;
            }

            if (enabledBackendMap.ContainsKey(backendDescriptor.BackendId))
                continue;

            enabledBackendMap.Add(backendDescriptor.BackendId, backendDescriptor);
            enabledBackends.Add(backendDescriptor);
        }

        enabledBackends = enabledBackends.OrderBy(x => x.BackendId).ToList();

        var enabledOptimizers = new List<RuntimeOptimizerDescriptor>();
        foreach (var optimizer in buildPlan.OptimizerDirectives.Where(x => x.Enabled).OrderBy(x => x.Name, StringComparer.Ordinal).ThenBy(x => x.Target))
        {
            if (!HasApplicableEnabledBackend(optimizer.Target, enabledBackendMap.Values))
                continue;

            if (!registry.TryResolveOptimizer(optimizer.Name, out var descriptor))
            {
                diagnostics.Add(new DialectDiagnostic(
                    "R003",
                    $"Runtime optimizer descriptor '{optimizer.Name}' was not registered.",
                    DialectDiagnosticSeverity.Error));
                continue;
            }

            if (!enabledOptimizers.Contains(descriptor))
                enabledOptimizers.Add(descriptor);
        }

        var allowedIntrinsics = new List<RuntimeIntrinsicDescriptor>();
        var allowedIntrinsicKeys = new HashSet<(string CanonicalId, DialectBackendSelector Target)>();
        foreach (var intrinsic in buildPlan.IntrinsicDirectives.Where(x => x.Allowed).OrderBy(x => x.Name, StringComparer.Ordinal).ThenBy(x => x.Target))
        {
            var applicableBackends = GetApplicableEnabledBackends(intrinsic.Target, enabledBackendMap.Values, registry, diagnostics, intrinsic.Name, "R004");
            foreach (var backend in applicableBackends)
            {
                if (!TryResolveIntrinsicForBackend(registry, intrinsic.Name, backend.BackendId, out var resolved))
                {
                    diagnostics.Add(new DialectDiagnostic(
                        "R004",
                        $"Runtime intrinsic descriptor '{intrinsic.Name}' for '{DialectBackendSelectorText.ToText(backend.BackendId)}' was not registered.",
                        DialectDiagnosticSeverity.Error));
                    continue;
                }

                var key = (resolved.CanonicalId, resolved.Target);
                if (allowedIntrinsicKeys.Add(key))
                    allowedIntrinsics.Add(resolved);
            }
        }

        var validation = new DialectValidationResult(diagnostics);

        return new DialectRuntimeComposition(
            buildPlan.Name,
            orderedModules,
            enabledBackends,
            enabledOptimizers,
            allowedIntrinsics.OrderBy(x => x.CanonicalId, StringComparer.Ordinal).ThenBy(x => x.Target).ToList(),
            validation);
    }

    private static bool HasApplicableEnabledBackend(DialectBackendSelector selector, IEnumerable<RuntimeBackendDescriptor> enabledBackends)
    {
        return enabledBackends.Any(x => selector.Matches(x.BackendId));
    }

    private static IReadOnlyList<RuntimeBackendDescriptor> GetApplicableEnabledBackends(
        DialectBackendSelector selector,
        IEnumerable<RuntimeBackendDescriptor> enabledBackends,
        DialectRuntimeDescriptorRegistry registry,
        List<DialectDiagnostic> diagnostics,
        string intrinsicName,
        string code)
    {
        if (selector.IsAny)
            return enabledBackends.OrderBy(x => x.BackendId).ToList();

        var enabledMatches = enabledBackends.Where(x => selector.Matches(x.BackendId)).OrderBy(x => x.BackendId).ToList();
        if (enabledMatches.Count > 0)
            return enabledMatches;

        return [];
    }

    private static bool TryResolveIntrinsicForBackend(
        DialectRuntimeDescriptorRegistry registry,
        string name,
        DialectBackendId backendId,
        out RuntimeIntrinsicDescriptor descriptor)
    {
        var candidates = registry.GetIntrinsicDescriptors(name);
        descriptor = candidates.FirstOrDefault(x => x.Target.IsAny || x.Target.BackendId == backendId)!;
        return descriptor != null;
    }
}
