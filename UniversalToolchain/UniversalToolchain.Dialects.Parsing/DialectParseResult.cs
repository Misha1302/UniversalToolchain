using System.Collections.ObjectModel;
using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Parsing;

/// <summary>
/// Represents deterministic parser output and diagnostics.
/// </summary>
public sealed class DialectParseResult
{
    private readonly ReadOnlyCollection<DialectDiagnostic> _diagnostics;

    public DialectParseResult(DialectSyntaxDocument? document, IEnumerable<DialectDiagnostic> diagnostics)
    {
        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        var snapshot = new List<DialectDiagnostic>();
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic == null)
                Thrower.Argument(nameof(diagnostics), "Diagnostics must not contain null entries.");

            snapshot.Add(diagnostic);
        }

        if (document == null && snapshot.Count == 0)
            Thrower.Argument(nameof(document), "Document can be null only when diagnostics contain parsing errors.");

        Document = document;
        _diagnostics = new ReadOnlyCollection<DialectDiagnostic>(snapshot);
    }

    public DialectSyntaxDocument? Document { get; }

    public IReadOnlyList<DialectDiagnostic> Diagnostics => _diagnostics;

    public bool IsSuccess => Document != null && _diagnostics.All(x => x.Severity != DialectDiagnosticSeverity.Error);
}
