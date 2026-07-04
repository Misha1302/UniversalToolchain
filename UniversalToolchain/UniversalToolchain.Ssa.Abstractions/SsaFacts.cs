using UniversalToolchain.Ir.Abstractions;

namespace UniversalToolchain.Ssa.Abstractions;

public static class SsaFacts
{
    public static FactId StructuralVerification { get; } = new("ssa.structural-verification");
}
