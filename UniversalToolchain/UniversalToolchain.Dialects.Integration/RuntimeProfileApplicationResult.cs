using System.Collections.ObjectModel;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class RuntimeProfileApplicationResult
{
    private readonly ReadOnlyCollection<DialectDiagnostic> _diagnostics;
    private readonly ReadOnlyCollection<RuntimeProfileProvenanceEntry> _provenance;

    public RuntimeProfileApplicationResult(
        RuntimeProfileDefinition profile,
        string sourceText,
        IEnumerable<DialectDiagnostic> diagnostics,
        IEnumerable<RuntimeProfileProvenanceEntry> provenance)
    {
        profile = profile.ArgNotNull();
        sourceText = sourceText.ArgNotNull();
        diagnostics = diagnostics.ArgNotNull();
        provenance = provenance.ArgNotNull();

        Profile = profile;
        SourceText = sourceText;
        _diagnostics = new ReadOnlyCollection<DialectDiagnostic>(diagnostics.Select(static x => x.NotNull()).ToList());
        _provenance = new ReadOnlyCollection<RuntimeProfileProvenanceEntry>(provenance.Select(static x => x.NotNull()).ToList());
    }

    public RuntimeProfileDefinition Profile { get; }

    public string SourceText { get; }

    public IReadOnlyList<DialectDiagnostic> Diagnostics => _diagnostics;

    public IReadOnlyList<RuntimeProfileProvenanceEntry> Provenance => _provenance;

    public bool CanCompose => !_diagnostics.Any(static x => x.Severity == DialectDiagnosticSeverity.Error);
}
