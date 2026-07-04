using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

public static class SsaOptimizationFacts
{
    public static FactId StructurallyVerifiedSsa => SsaFacts.StructuralVerification;

    public static FactId LocallyConstantFolded { get; } = new("ssa.optimization.constant-folded.local");
}
