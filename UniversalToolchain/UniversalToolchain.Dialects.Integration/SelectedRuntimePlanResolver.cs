using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class SelectedRuntimePlanResolver
{
    private readonly IRuntimeComponentCatalog _catalog;

    public SelectedRuntimePlanResolver(IRuntimeComponentCatalog catalog)
    {
        if (catalog == null)
            Thrower.ArgumentNull(nameof(catalog));

        _catalog = catalog;
    }

    public SelectedRuntimePlan Resolve(DialectBuildPlan buildPlan)
    {
        if (buildPlan == null)
            Thrower.ArgumentNull(nameof(buildPlan));

        var diagnostics = new List<DialectDiagnostic>(buildPlan.ValidationResult.Diagnostics);
        var modules = ResolveModules(buildPlan, diagnostics);
        var backends = ResolveBackends(buildPlan, diagnostics, out var selectedBackendIds);
        var optimizers = ResolveOptimizers(buildPlan, diagnostics, selectedBackendIds);

        return new SelectedRuntimePlan(modules, optimizers, backends, diagnostics);
    }

    private IReadOnlyList<RuntimeComponentManifestEntry> ResolveModules(DialectBuildPlan buildPlan, ICollection<DialectDiagnostic> diagnostics)
    {
        var modules = new List<RuntimeComponentManifestEntry>();
        foreach (var moduleAlias in buildPlan.OrderedModules)
        {
            if (!_catalog.TryResolveModule(moduleAlias, out var entry) || entry == null)
            {
                diagnostics.Add(new DialectDiagnostic("R001", $"Runtime module descriptor '{moduleAlias}' was not registered.", DialectDiagnosticSeverity.Error));
                continue;
            }

            modules.Add(entry);
        }

        return modules;
    }

    private IReadOnlyList<RuntimeComponentManifestEntry> ResolveBackends(
        DialectBuildPlan buildPlan,
        ICollection<DialectDiagnostic> diagnostics,
        out IReadOnlySet<DialectBackendId> selectedBackendIds)
    {
        var backends = new List<RuntimeComponentManifestEntry>();
        var backendIdSet = new SortedSet<DialectBackendId>();

        foreach (var backendId in buildPlan.EnabledBackends.OrderBy(x => x))
        {
            if (!_catalog.TryResolveBackend(backendId.Value, out var entry) || entry == null)
            {
                diagnostics.Add(new DialectDiagnostic("R002", $"Runtime backend descriptor '{backendId.Value}' was not registered.", DialectDiagnosticSeverity.Error));
                continue;
            }

            var resolvedBackendId = new DialectBackendId(entry.CanonicalAlias);
            if (!backendIdSet.Add(resolvedBackendId))
                continue;

            backends.Add(entry);
        }

        var orderedBackends = backends
            .OrderBy(x => x.CanonicalAlias, StringComparer.Ordinal)
            .ThenBy(x => x.ComponentId.Value, StringComparer.Ordinal)
            .ToList();

        selectedBackendIds = backendIdSet;
        return orderedBackends;
    }

    private IReadOnlyList<RuntimeComponentManifestEntry> ResolveOptimizers(
        DialectBuildPlan buildPlan,
        ICollection<DialectDiagnostic> diagnostics,
        IReadOnlySet<DialectBackendId> selectedBackendIds)
    {
        var optimizers = new List<RuntimeComponentManifestEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var optimizer in buildPlan.OptimizerDirectives
                     .Where(x => x.Enabled)
                     .OrderBy(x => x.Name, StringComparer.Ordinal)
                     .ThenBy(x => x.Target))
        {
            if (!selectedBackendIds.Any(optimizer.Target.Matches))
                continue;

            if (!_catalog.TryResolveOptimizer(optimizer.Name, out var entry) || entry == null)
            {
                diagnostics.Add(new DialectDiagnostic("R003", $"Runtime optimizer descriptor '{optimizer.Name}' was not registered.", DialectDiagnosticSeverity.Error));
                continue;
            }

            if (seen.Add(entry.CanonicalAlias))
                optimizers.Add(entry);
        }

        return optimizers;
    }
}