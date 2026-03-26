using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

public sealed class DialectRuntimeSelectionResolver
{
    private readonly IDialectRuntimeCatalog _catalog;

    public DialectRuntimeSelectionResolver(IDialectRuntimeCatalog catalog)
    {
        _catalog = catalog;
    }

    public DialectResolvedRuntimeSelection Resolve(DialectBuildPlan plan)
    {
        var diagnostics = new List<DialectDiagnostic>(plan.ValidationResult.Diagnostics);

        var orderedModules = new List<DialectRuntimeModuleDescriptor>();
        foreach (var moduleAlias in plan.OrderedModules)
        {
            if (!_catalog.TryResolveModule(moduleAlias, out var descriptor) || descriptor == null)
            {
                diagnostics.Add(new DialectDiagnostic("R001", $"Runtime module descriptor '{moduleAlias}' was not registered.", DialectDiagnosticSeverity.Error));
                continue;
            }

            orderedModules.Add(descriptor);
        }

        var enabledBackends = new List<DialectRuntimeBackendDescriptor>();
        foreach (var backend in plan.EnabledBackends.OrderBy(x => x))
        {
            if (!_catalog.TryResolveBackend(backend, out var descriptor) || descriptor == null)
            {
                diagnostics.Add(new DialectDiagnostic("R002", $"Runtime backend descriptor '{backend.Value}' was not registered.", DialectDiagnosticSeverity.Error));
                continue;
            }

            if (enabledBackends.All(x => x.CanonicalId != descriptor.CanonicalId))
                enabledBackends.Add(descriptor);
        }

        var enabledOptimizers = new List<DialectRuntimeOptimizerDescriptor>();
        foreach (var optimizer in plan.OptimizerDirectives.Where(x => x.Enabled).OrderBy(x => x.Name, StringComparer.Ordinal).ThenBy(x => x.Target))
        {
            if (!_catalog.TryResolveOptimizer(optimizer.Name, out var descriptor) || descriptor == null)
            {
                diagnostics.Add(new DialectDiagnostic("R003", $"Runtime optimizer descriptor '{optimizer.Name}' was not registered.", DialectDiagnosticSeverity.Error));
                continue;
            }

            if (enabledOptimizers.All(x => x.CanonicalAlias != descriptor.CanonicalAlias))
                enabledOptimizers.Add(descriptor);
        }

        return new DialectResolvedRuntimeSelection(orderedModules, enabledOptimizers, enabledBackends, diagnostics);
    }
}
