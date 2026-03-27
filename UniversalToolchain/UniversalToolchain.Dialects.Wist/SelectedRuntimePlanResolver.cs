using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

public sealed class SelectedRuntimePlanResolver
{
    private readonly IWistRuntimeManifest _manifest;

    public SelectedRuntimePlanResolver(IWistRuntimeManifest manifest)
    {
        _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
    }

    public SelectedRuntimePlan Resolve(DialectBuildPlan buildPlan)
    {
        if (buildPlan == null)
            Thrower.ArgumentNull(nameof(buildPlan));

        var diagnostics = new List<DialectDiagnostic>(buildPlan.ValidationResult.Diagnostics);
        var modules = new List<RuntimeComponentManifestEntry>();
        foreach (var moduleAlias in buildPlan.OrderedModules)
        {
            if (!_manifest.TryResolveModule(moduleAlias, out var entry) || entry == null)
            {
                diagnostics.Add(new DialectDiagnostic("R001", $"Runtime module descriptor '{moduleAlias}' was not registered.", DialectDiagnosticSeverity.Error));
                continue;
            }

            modules.Add(entry);
        }

        var backends = new List<RuntimeComponentManifestEntry>();
        foreach (var backendId in buildPlan.EnabledBackends.OrderBy(x => x))
        {
            if (!_manifest.TryResolveBackend(backendId.Value, out var entry) || entry == null)
            {
                diagnostics.Add(new DialectDiagnostic("R002", $"Runtime backend descriptor '{backendId.Value}' was not registered.", DialectDiagnosticSeverity.Error));
                continue;
            }

            if (!backends.Contains(entry))
                backends.Add(entry);
        }

        var optimizers = new List<RuntimeComponentManifestEntry>();
        foreach (var optimizer in buildPlan.OptimizerDirectives.Where(x => x.Enabled).OrderBy(x => x.Name, StringComparer.Ordinal).ThenBy(x => x.Target))
        {
            if (!backends.Any(x => optimizer.Target.Matches(new DialectBackendId(x.CanonicalAlias))))
                continue;

            if (!_manifest.TryResolveOptimizer(optimizer.Name, out var entry) || entry == null)
            {
                diagnostics.Add(new DialectDiagnostic("R003", $"Runtime optimizer descriptor '{optimizer.Name}' was not registered.", DialectDiagnosticSeverity.Error));
                continue;
            }

            if (!optimizers.Contains(entry))
                optimizers.Add(entry);
        }

        return new SelectedRuntimePlan(modules, optimizers, backends.OrderBy(x => x.CanonicalAlias, StringComparer.Ordinal).ToList(), diagnostics);
    }
}
