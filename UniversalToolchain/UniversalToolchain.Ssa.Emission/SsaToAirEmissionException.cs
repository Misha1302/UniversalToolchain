using UniversalToolchain.Ir.Abstractions;

namespace UniversalToolchain.Ssa.Emission;

public sealed class SsaToAirEmissionException : InvalidOperationException
{
    public SsaToAirEmissionException(IEnumerable<IrDiagnostic> diagnostics)
        : base("SSA to AIR emission failed: " + string.Join("; ", diagnostics.Select(static x => $"{x.Code}: {x.Message}")))
    {
        Diagnostics = diagnostics.ToArray();
    }

    public IReadOnlyList<IrDiagnostic> Diagnostics { get; }
}
