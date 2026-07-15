using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Emission;

/// <summary>
/// Detects SSA values that the minimal stack-based AIR emitter would need to
/// duplicate or spill inside one block.
///
/// The preflight is intentionally conservative. If a function contains an
/// operation, call-lowering target, or terminator shape outside the current
/// emitter surface, it yields no diagnostics and lets the existing specialized
/// emission diagnostic own that failure.
/// </summary>
internal static class SsaToAirValueReusePreflight
{
    public const string DiagnosticCode = "ssa.to-air.value-reuse.unsupported";

    public static IReadOnlyList<IrDiagnostic> Analyze(
        SsaFunction function,
        SsaCallableLoweringPlanner callLoweringPlanner)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(callLoweringPlanner);

        var perBlock = new List<(SsaBlock Block, Dictionary<SsaValueId, List<string>> Uses)>();
        foreach (var block in function.Blocks)
        {
            var uses = new Dictionary<SsaValueId, List<string>>();
            if (!TryCollectBlockUses(block, callLoweringPlanner, uses))
                return [];

            perBlock.Add((block, uses));
        }

        return perBlock
            .SelectMany(static item => item.Uses
                .Where(static pair => pair.Value.Count > 1)
                .OrderBy(static pair => pair.Key)
                .Select(pair => new IrDiagnostic(
                    IrDiagnosticSeverity.Error,
                    DiagnosticCode,
                    $"SSA value '{pair.Key}' is required at {pair.Value.Count} stack positions in block '{item.Block.Id}' " +
                    $"({string.Join("; ", pair.Value)}); minimal AIR emission has no duplication or spill scheduling.")))
            .ToArray();
    }

    private static bool TryCollectBlockUses(
        SsaBlock block,
        SsaCallableLoweringPlanner callLoweringPlanner,
        Dictionary<SsaValueId, List<string>> uses)
    {
        foreach (var instruction in block.Instructions)
        {
            switch (instruction)
            {
                case SsaOperation operation when IsSupportedStackOperation(operation):
                    break;
                case SsaCall call when callLoweringPlanner.TrySelect(call, out _, out _):
                    AddUses(uses, call.Operands, index => $"call '{call.Id}' operand {index}");
                    break;
                default:
                    return false;
            }
        }

        return TryCollectTerminatorUses(block, uses);
    }

    private static bool TryCollectTerminatorUses(
        SsaBlock block,
        Dictionary<SsaValueId, List<string>> uses)
    {
        var terminator = block.Terminator;
        if (terminator is null)
            return false;

        switch (terminator.Kind)
        {
            case SsaTerminatorKind.Return:
                AddUses(uses, terminator.Operands, static index => $"return operand {index}");
                return true;
            case SsaTerminatorKind.Jump when terminator.Transfers.Count == 1:
            {
                var transfer = terminator.Transfers.Single();
                AddUses(
                    uses,
                    transfer.Arguments,
                    index => $"jump argument {index} to '{transfer.Target}'");
                return true;
            }
            case SsaTerminatorKind.Branch when
                terminator.Operands.Count == 1 &&
                terminator.Transfers.Count == 2 &&
                terminator.Transfers[0].Arguments.SequenceEqual(terminator.Transfers[1].Arguments):
            {
                var sharedArguments = terminator.Transfers[0].Arguments;
                AddUses(uses, sharedArguments, static index => $"branch shared argument {index}");
                AddUse(uses, terminator.Operands.Single(), "branch condition");
                return true;
            }
            default:
                return false;
        }
    }

    private static bool IsSupportedStackOperation(SsaOperation operation) =>
        operation.Operands.Count == 0 &&
        operation.Results.Count == 1 &&
        (operation.OpId == SsaOperations.ConstantInt32 ||
         operation.OpId == SsaOperations.ConstantBool ||
         operation.OpId == SsaOperations.ConstantFloat64 ||
         operation.OpId == SsaOperations.LoadExternalInt32 ||
         operation.OpId == SsaOperations.LoadExternalBool ||
         operation.OpId == SsaOperations.LoadExternalFloat64);

    private static void AddUses(
        Dictionary<SsaValueId, List<string>> uses,
        IReadOnlyList<SsaValueId> values,
        Func<int, string> describe)
    {
        for (var index = 0; index < values.Count; index++)
            AddUse(uses, values[index], describe(index));
    }

    private static void AddUse(
        Dictionary<SsaValueId, List<string>> uses,
        SsaValueId value,
        string site)
    {
        if (!uses.TryGetValue(value, out var sites))
        {
            sites = [];
            uses.Add(value, sites);
        }

        sites.Add(site);
    }
}
