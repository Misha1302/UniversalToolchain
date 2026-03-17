using System.Collections.ObjectModel;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
/// Represents semantic validation result for one dialect definition.
/// </summary>
public sealed class DialectValidationResult
{
    private readonly ReadOnlyCollection<DialectDiagnostic> _diagnostics;

    public DialectValidationResult(IEnumerable<DialectDiagnostic>? diagnostics = null)
    {
        var list = new List<DialectDiagnostic>();
        if (diagnostics != null)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic == null)
                    Thrower.Argument(nameof(diagnostics), "Diagnostics collection must not contain null entries.");

                list.Add(diagnostic);
            }
        }

        _diagnostics = new ReadOnlyCollection<DialectDiagnostic>(list);
    }

    public IReadOnlyList<DialectDiagnostic> Diagnostics => _diagnostics;

    public bool IsValid => _diagnostics.All(x => x.Severity != DialectDiagnosticSeverity.Error);
}
