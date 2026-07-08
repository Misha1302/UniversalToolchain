using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

public static class SsaOptimizationFacts
{
    public static FactId StructurallyVerifiedSsa => SsaFacts.StructuralVerification;

    public static FactId LocallyConstantFolded { get; } = new("ssa.optimization.constant-folded.local");

    public static FactId SparseConditionalConstantPropagated { get; } = new("ssa.optimization.sccp-lite");

    public static FactId DeadPureInstructionsEliminated { get; } = new("ssa.optimization.dead-pure-instructions-eliminated");

    public static FactId BranchesFolded { get; } = new("ssa.optimization.branches-folded");

    public static FactId UnreachableBlocksEliminated { get; } = new("ssa.optimization.unreachable-blocks-eliminated");
}
