using UniversalToolchain.Ir.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

public sealed class SsaOptimizationException : InvalidOperationException
{
    public SsaOptimizationException(string message, IEnumerable<IrDiagnostic> diagnostics)
        : base(message + ": " + string.Join("; ", diagnostics.Select(static x => $"{x.Code}: {x.Message}")))
    {
        Diagnostics = diagnostics.ToArray();
    }

    public IReadOnlyList<IrDiagnostic> Diagnostics { get; }
}
