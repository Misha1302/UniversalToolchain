using System.Collections.ObjectModel;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Immutable typed overlay applied to an already compiled dialect plan. It is never rendered back
/// into executable DSL; textual rendering is reserved for explain/export surfaces.
/// </summary>
public sealed class DialectPlanOverlay
{
    public DialectPlanOverlay(
        IEnumerable<string>? addedModules = null,
        IEnumerable<DialectBackendId>? addedBackends = null,
        IEnumerable<OptimizerBuildDirective>? addedOptimizers = null,
        SecurityProfile? defaultSecurityProfile = null,
        IEnumerable<KeyValuePair<string, bool>>? defaultCapabilities = null,
        IEnumerable<DialectDiagnostic>? diagnostics = null,
        IEnumerable<RuntimeProfileProvenanceEntry>? provenance = null)
    {
        AddedModules = new ReadOnlyCollection<string>((addedModules ?? []).Distinct(StringComparer.Ordinal).OrderBy(static x => x, StringComparer.Ordinal).ToArray());
        AddedBackends = new ReadOnlyCollection<DialectBackendId>((addedBackends ?? []).Distinct().OrderBy(static x => x).ToArray());
        AddedOptimizers = new ReadOnlyCollection<OptimizerBuildDirective>((addedOptimizers ?? []).OrderBy(static x => x.Name, StringComparer.Ordinal).ThenBy(static x => x.Target).ToArray());
        DefaultSecurityProfile = defaultSecurityProfile;
        DefaultCapabilities = new ReadOnlyDictionary<string, bool>((defaultCapabilities ?? []).ToDictionary(static x => x.Key, static x => x.Value, StringComparer.Ordinal));
        Diagnostics = new ReadOnlyCollection<DialectDiagnostic>((diagnostics ?? []).ToArray());
        Provenance = new ReadOnlyCollection<RuntimeProfileProvenanceEntry>((provenance ?? []).ToArray());
    }

    public IReadOnlyList<string> AddedModules { get; }
    public IReadOnlyList<DialectBackendId> AddedBackends { get; }
    public IReadOnlyList<OptimizerBuildDirective> AddedOptimizers { get; }
    public SecurityProfile? DefaultSecurityProfile { get; }
    public IReadOnlyDictionary<string, bool> DefaultCapabilities { get; }
    public IReadOnlyList<DialectDiagnostic> Diagnostics { get; }
    public IReadOnlyList<RuntimeProfileProvenanceEntry> Provenance { get; }
    public bool CanApply => Diagnostics.All(static diagnostic => diagnostic.Severity != DialectDiagnosticSeverity.Error);

    public DialectBuildPlan Apply(DialectBuildPlan baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);

        var modules = baseline.OrderedModules.Concat(AddedModules).Distinct(StringComparer.Ordinal).ToArray();
        var backends = baseline.EnabledBackends.Concat(AddedBackends).Distinct().OrderBy(static x => x).ToArray();
        var optimizerKeys = new HashSet<(string Name, DialectBackendSelector Target)>(
            baseline.OptimizerDirectives.Select(static directive => (directive.Name, directive.Target)));
        var optimizers = baseline.OptimizerDirectives.ToList();
        foreach (var optimizer in AddedOptimizers)
        {
            if (optimizerKeys.Add((optimizer.Name, optimizer.Target)))
                optimizers.Add(optimizer);
        }

        var capabilities = new Dictionary<string, bool>(baseline.Capabilities, StringComparer.Ordinal);
        foreach (var capability in DefaultCapabilities)
            capabilities.TryAdd(capability.Key, capability.Value);

        return new DialectBuildPlan(
            baseline.Name,
            baseline.Version,
            modules,
            backends,
            baseline.DisabledBackends,
            baseline.IntrinsicDirectives,
            optimizers,
            baseline.SecurityProfile ?? DefaultSecurityProfile,
            capabilities,
            new DialectValidationResult(baseline.ValidationResult.Diagnostics.Concat(Diagnostics)));
    }
}
