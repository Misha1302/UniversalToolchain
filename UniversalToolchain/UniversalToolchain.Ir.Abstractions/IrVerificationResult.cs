using System.Collections.ObjectModel;

namespace UniversalToolchain.Ir.Abstractions;

public sealed class IrVerificationResult
{
    private readonly ReadOnlyCollection<IrDiagnostic> _diagnostics;

    public IrVerificationResult(IEnumerable<IrDiagnostic>? diagnostics = null)
    {
        _diagnostics = new ReadOnlyCollection<IrDiagnostic>((diagnostics ?? []).ToList());
    }

    public static IrVerificationResult Success { get; } = new();

    public IReadOnlyList<IrDiagnostic> Diagnostics => _diagnostics;

    public bool IsSuccess => _diagnostics.All(static x => x.Severity != IrDiagnosticSeverity.Error);
}
