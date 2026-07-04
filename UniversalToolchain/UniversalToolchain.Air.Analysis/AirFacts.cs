using UniversalToolchain.Ir.Abstractions;

namespace UniversalToolchain.Air.Analysis;

public static class AirFacts
{
    public static FactId ControlFlowGraph { get; } = new("air.cfg");

    public static FactId StructuralVerification { get; } = new("air.structural-verification");
}
