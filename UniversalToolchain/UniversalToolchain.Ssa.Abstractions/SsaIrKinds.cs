using UniversalToolchain.Ir.Abstractions;

namespace UniversalToolchain.Ssa.Abstractions;

public static class SsaIrKinds
{
    public static IrKind Ssa { get; } = new("ssa");
}
