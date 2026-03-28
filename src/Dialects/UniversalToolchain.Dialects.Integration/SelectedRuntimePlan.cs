using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed record SelectedRuntimePlan(
    IReadOnlyList<RuntimeComponentManifestEntry> OrderedModules,
    IReadOnlyList<RuntimeComponentManifestEntry> EnabledOptimizers,
    IReadOnlyList<RuntimeComponentManifestEntry> EnabledBackends,
    IReadOnlyList<DialectDiagnostic> Diagnostics) : IDialectRuntimeSelection
{
    public bool IsResolved => Diagnostics.All(x => x.Severity != DialectDiagnosticSeverity.Error);
}