using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public sealed record SelectedRuntimePlan(
    IReadOnlyList<RuntimeComponentManifestEntry> OrderedModules,
    IReadOnlyList<RuntimeComponentManifestEntry> EnabledOptimizers,
    IReadOnlyList<RuntimeComponentManifestEntry> EnabledBackends,
    IReadOnlyList<DialectDiagnostic> Diagnostics) : IDialectRuntimeSelection
{
    public bool IsResolved => Diagnostics.All(x => x.Severity != DialectDiagnosticSeverity.Error);
}
