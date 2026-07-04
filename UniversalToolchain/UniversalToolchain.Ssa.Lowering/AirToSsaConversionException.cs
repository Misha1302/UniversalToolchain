using UniversalToolchain.Ir.Abstractions;

namespace UniversalToolchain.Ssa.Lowering;

public sealed class AirToSsaConversionException : InvalidOperationException
{
    public AirToSsaConversionException(IEnumerable<IrDiagnostic> diagnostics)
        : base("AIR to SSA conversion failed: " + string.Join("; ", diagnostics.Select(static x => $"{x.Code}: {x.Message}")))
    {
        Diagnostics = diagnostics.ToArray();
    }

    public IReadOnlyList<IrDiagnostic> Diagnostics { get; }
}
