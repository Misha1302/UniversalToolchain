using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

public sealed record SelectedRuntimePlan(
    IReadOnlyList<RuntimeComponentManifestEntry> OrderedModules,
    IReadOnlyList<RuntimeComponentManifestEntry> EnabledOptimizers,
    IReadOnlyList<RuntimeComponentManifestEntry> EnabledBackends,
    IReadOnlyList<DialectDiagnostic> Diagnostics)
{
    public bool IsResolved => Diagnostics.All(x => x.Severity != DialectDiagnosticSeverity.Error);
}
