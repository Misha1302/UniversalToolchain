using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

public sealed record DialectResolvedRuntimeSelection(
    IReadOnlyList<DialectRuntimeModuleDescriptor> OrderedModules,
    IReadOnlyList<DialectRuntimeOptimizerDescriptor> EnabledOptimizers,
    IReadOnlyList<DialectRuntimeBackendDescriptor> EnabledBackends,
    IReadOnlyList<DialectDiagnostic> Diagnostics)
{
    public bool IsResolved => Diagnostics.All(x => x.Severity != DialectDiagnosticSeverity.Error);
}
