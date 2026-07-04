using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Core;

public sealed class StructuralSsaVerifier : IIrVerifier
{
    private readonly SsaDescriptorSet _descriptors;
    private readonly SsaSemanticCallVerifier _callVerifier;

    public StructuralSsaVerifier(SsaDescriptorSet? descriptors = null, SemanticDescriptorSet? semanticDescriptors = null)
    {
        _descriptors = descriptors ?? SsaDescriptorSet.Empty;
        _callVerifier = new SsaSemanticCallVerifier(semanticDescriptors ?? SemanticDescriptorSet.Empty);
    }

    public IrKind Kind => SsaIrKinds.Ssa;

    public IrVerificationResult Verify(IIrArtifact artifact, IrPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (artifact is not SsaArtifact ssaArtifact)
        {
            return Error(
                "ssa.artifact.type",
                $"Expected SSA artifact, got artifact kind '{artifact.Kind}'.");
        }

        var diagnostics = new List<IrDiagnostic>();
        VerifyModule(ssaArtifact.Module, diagnostics);
        return diagnostics.Count == 0 ? IrVerificationResult.Success : new IrVerificationResult(diagnostics);
    }

    private void VerifyModule(SsaModule module, List<IrDiagnostic> diagnostics)
    {
        var functionIds = new HashSet<SsaFunctionId>();
        foreach (var function in module.Functions)
        {
            if (!functionIds.Add(function.Id))
            {
                diagnostics.Add(Diagnostic(
                    "ssa.function.duplicate",
                    $"Module '{module.Id}' defines duplicate SSA function '{function.Id}'."));
                continue;
            }

            VerifyFunction(module.Id, function, diagnostics);
        }
    }

    private void VerifyFunction(SsaModuleId moduleId, SsaFunction function, List<IrDiagnostic> diagnostics)
    {
        var blocks = BuildBlockMap(moduleId, function, diagnostics);
        if (!blocks.TryGetValue(function.EntryBlockId, out var entryBlock))
        {
            diagnostics.Add(Diagnostic(
                "ssa.entry.missing",
                $"Function '{function.Id}' entry block '{function.EntryBlockId}' does not exist."));
            return;
        }

        var state = BuildDefinitionState(function, diagnostics);
        VerifyTerminators(function, blocks, state, diagnostics);
        VerifyInstructions(function, state, diagnostics);

        var reachable = ComputeReachable(entryBlock.Id, blocks);
        var dominators = ComputeDominators(entryBlock.Id, blocks, reachable);
        VerifyUses(function, blocks, state, reachable, dominators, diagnostics);
    }

    private static Dictionary<SsaBlockId, SsaBlock> BuildBlockMap(
        SsaModuleId moduleId,
        SsaFunction function,
        List<IrDiagnostic> diagnostics)
    {
        var blocks = new Dictionary<SsaBlockId, SsaBlock>();
        foreach (var block in function.Blocks)
        {
            if (!blocks.TryAdd(block.Id, block))
            {
                diagnostics.Add(Diagnostic(
                    "ssa.block.duplicate",
                    $"Function '{function.Id}' defines duplicate SSA block '{block.Id}' in module '{moduleId}'."));
            }
        }

        return blocks;
    }

    private static DefinitionState BuildDefinitionState(SsaFunction function, List<IrDiagnostic> diagnostics)
    {
        var state = new DefinitionState();

        foreach (var parameter in function.Parameters)
        {
            AddValueDefinition(
                state,
                parameter.Value,
                function.EntryBlockId,
                instructionIndex: -1,
                owner: $"function '{function.Id}' parameter",
                diagnostics);
        }

        foreach (var block in function.Blocks)
        {
            foreach (var parameter in block.Parameters)
            {
                AddValueDefinition(
                    state,
                    parameter.Value,
                    block.Id,
                    instructionIndex: -1,
                    owner: $"block '{block.Id}' parameter",
                    diagnostics);
            }

            for (var index = 0; index < block.Instructions.Count; index++)
            {
                var instruction = block.Instructions[index];
                if (!state.InstructionIds.Add(instruction.Id))
                {
                    diagnostics.Add(Diagnostic(
                        "ssa.instruction.duplicate",
                        $"Function '{function.Id}' defines duplicate SSA instruction '{instruction.Id}'."));
                }

                foreach (var result in instruction.Results)
                {
                    AddValueDefinition(
                        state,
                        result,
                        block.Id,
                        index,
                        owner: $"instruction '{instruction.Id}' result",
                        diagnostics);
                }
            }
        }

        return state;
    }

    private static void AddValueDefinition(
        DefinitionState state,
        SsaValue value,
        SsaBlockId blockId,
        int instructionIndex,
        string owner,
        List<IrDiagnostic> diagnostics)
    {
        if (!state.Values.TryAdd(value.Id, new ValueDefinition(value, blockId, instructionIndex)))
        {
            diagnostics.Add(Diagnostic(
                "ssa.value.duplicate",
                $"SSA value '{value.Id}' is defined more than once; duplicate owner is {owner}."));
        }
    }

    private void VerifyInstructions(SsaFunction function, DefinitionState state, List<IrDiagnostic> diagnostics)
    {
        var visibleValues = state.Values.ToDictionary(static x => x.Key, static x => x.Value.Value);

        foreach (var block in function.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                switch (instruction)
                {
                    case SsaOperation operation:
                        VerifyOperation(operation, state, diagnostics);
                        break;
                    case SsaCall call:
                        VerifyCall(call, visibleValues, diagnostics);
                        break;
                    default:
                        diagnostics.Add(Diagnostic(
                            "ssa.instruction.unsupported",
                            $"Instruction '{instruction.Id}' has unsupported SSA instruction shape '{instruction.GetType().Name}'."));
                        break;
                }
            }
        }
    }

    private void VerifyOperation(SsaOperation operation, DefinitionState state, List<IrDiagnostic> diagnostics)
    {
        if (!_descriptors.TryGet(operation.OpId, out var descriptor))
        {
            diagnostics.Add(Diagnostic(
                "ssa.operation.descriptor.missing",
                $"Operation '{operation.Id}' uses unknown SSA operation descriptor '{operation.OpId}'."));
            return;
        }

        if (operation.Operands.Count != descriptor.OperandTypes.Count)
        {
            diagnostics.Add(Diagnostic(
                "ssa.operation.operand-count",
                $"Operation '{operation.Id}' expects {descriptor.OperandTypes.Count} operands but has {operation.Operands.Count}."));
        }

        if (operation.Results.Count != descriptor.ResultTypes.Count)
        {
            diagnostics.Add(Diagnostic(
                "ssa.operation.result-count",
                $"Operation '{operation.Id}' expects {descriptor.ResultTypes.Count} results but has {operation.Results.Count}."));
        }

        VerifyOperandTypes(operation, descriptor, state, diagnostics);
        VerifyResultTypes(operation, descriptor, diagnostics);
        VerifyAttributes(operation, descriptor, diagnostics);
    }

    private void VerifyCall(
        SsaCall call,
        IReadOnlyDictionary<SsaValueId, SsaValue> visibleValues,
        List<IrDiagnostic> diagnostics)
    {
        var result = _callVerifier.Verify(call, visibleValues);
        diagnostics.AddRange(result.Diagnostics);
    }

    private static void VerifyOperandTypes(
        SsaOperation operation,
        SsaOpDescriptor descriptor,
        DefinitionState state,
        List<IrDiagnostic> diagnostics)
    {
        var count = Math.Min(operation.Operands.Count, descriptor.OperandTypes.Count);
        for (var index = 0; index < count; index++)
        {
            if (!state.Values.TryGetValue(operation.Operands[index], out var definition))
                continue;

            var expected = descriptor.OperandTypes[index];
            if (definition.Value.Type != expected)
            {
                diagnostics.Add(Diagnostic(
                    "ssa.operation.operand-type",
                    $"Operation '{operation.Id}' operand {index} expects type '{expected}' but value '{operation.Operands[index]}' has type '{definition.Value.Type}'."));
            }
        }
    }

    private static void VerifyResultTypes(
        SsaOperation operation,
        SsaOpDescriptor descriptor,
        List<IrDiagnostic> diagnostics)
    {
        var count = Math.Min(operation.Results.Count, descriptor.ResultTypes.Count);
        for (var index = 0; index < count; index++)
        {
            var expected = descriptor.ResultTypes[index];
            if (operation.Results[index].Type != expected)
            {
                diagnostics.Add(Diagnostic(
                    "ssa.operation.result-type",
                    $"Operation '{operation.Id}' result {index} expects type '{expected}' but value '{operation.Results[index].Id}' has type '{operation.Results[index].Type}'."));
            }
        }
    }

    private static void VerifyAttributes(
        SsaOperation operation,
        SsaOpDescriptor descriptor,
        List<IrDiagnostic> diagnostics)
    {
        foreach (var required in descriptor.RequiredAttributes)
        {
            if (!operation.Attributes.Contains(required))
            {
                diagnostics.Add(Diagnostic(
                    "ssa.operation.attribute.missing",
                    $"Operation '{operation.Id}' is missing required SSA attribute '{required}'."));
            }
        }

        foreach (var attribute in operation.Attributes.Values)
        {
            if (!descriptor.AllowedAttributes.Contains(attribute.Key))
            {
                diagnostics.Add(Diagnostic(
                    "ssa.operation.attribute.unknown",
                    $"Operation '{operation.Id}' has attribute '{attribute.Key}' that is not allowed by descriptor '{descriptor.Id}'."));
            }
        }
    }

    private static void VerifyTerminators(
        SsaFunction function,
        IReadOnlyDictionary<SsaBlockId, SsaBlock> blocks,
        DefinitionState state,
        List<IrDiagnostic> diagnostics)
    {
        foreach (var block in function.Blocks)
        {
            if (block.Terminator is null)
            {
                diagnostics.Add(Diagnostic(
                    "ssa.terminator.missing",
                    $"Block '{block.Id}' in function '{function.Id}' has no terminator."));
                continue;
            }

            VerifyTerminatorShape(function, block, block.Terminator, state, diagnostics);
            foreach (var transfer in block.Terminator.Transfers)
            {
                if (!blocks.TryGetValue(transfer.Target, out var target))
                {
                    diagnostics.Add(Diagnostic(
                        "ssa.terminator.target.missing",
                        $"Block '{block.Id}' branches to unknown target block '{transfer.Target}'."));
                    continue;
                }

                VerifyBlockArguments(function, block.Id, transfer, target, state, diagnostics);
            }
        }
    }

    private static void VerifyTerminatorShape(
        SsaFunction function,
        SsaBlock block,
        SsaTerminator terminator,
        DefinitionState state,
        List<IrDiagnostic> diagnostics)
    {
        var expectedTransfers = terminator.Kind switch
        {
            SsaTerminatorKind.Return => 0,
            SsaTerminatorKind.Jump => 1,
            SsaTerminatorKind.Branch => 2,
            SsaTerminatorKind.Unreachable => 0,
            _ => 0
        };

        int? expectedOperands = terminator.Kind == SsaTerminatorKind.Branch ? 1 : null;
        if (terminator.Transfers.Count != expectedTransfers)
        {
            diagnostics.Add(Diagnostic(
                "ssa.terminator.target-count",
                $"Terminator in block '{block.Id}' expects {expectedTransfers} targets for kind '{terminator.Kind}' but has {terminator.Transfers.Count}."));
        }

        if (expectedOperands is { } expected && terminator.Operands.Count != expected)
        {
            diagnostics.Add(Diagnostic(
                "ssa.terminator.operand-count",
                $"Terminator in block '{block.Id}' expects {expected} operands for kind '{terminator.Kind}' but has {terminator.Operands.Count}."));
        }

        if (terminator.Kind == SsaTerminatorKind.Branch &&
            terminator.Operands.Count > 0 &&
            state.Values.TryGetValue(terminator.Operands[0], out var condition) &&
            condition.Value.Type != SsaTypes.Bool)
        {
            diagnostics.Add(Diagnostic(
                "ssa.branch.condition-type",
                $"Branch terminator in block '{block.Id}' expects bool condition but value '{terminator.Operands[0]}' has type '{condition.Value.Type}'."));
        }

        if (terminator.Kind != SsaTerminatorKind.Return)
            return;

        if (function.ReturnType is null)
        {
            if (terminator.Operands.Count != 0)
            {
                diagnostics.Add(Diagnostic(
                    "ssa.return.unexpected-value",
                    $"Function '{function.Id}' has no return type but block '{block.Id}' returns values."));
            }

            return;
        }

        if (terminator.Operands.Count != 1)
        {
            diagnostics.Add(Diagnostic(
                "ssa.return.value-count",
                $"Function '{function.Id}' returns '{function.ReturnType}' but block '{block.Id}' returns {terminator.Operands.Count} values."));
            return;
        }

        if (!state.Values.TryGetValue(terminator.Operands[0], out var definition))
            return;

        if (definition.Value.Type != function.ReturnType)
        {
            diagnostics.Add(Diagnostic(
                "ssa.return.type",
                $"Function '{function.Id}' returns '{function.ReturnType}' but block '{block.Id}' returns value '{terminator.Operands[0]}' of type '{definition.Value.Type}'."));
        }
    }

    private static void VerifyBlockArguments(
        SsaFunction function,
        SsaBlockId sourceBlockId,
        SsaBlockTransfer transfer,
        SsaBlock target,
        DefinitionState state,
        List<IrDiagnostic> diagnostics)
    {
        if (target.Parameters.Count != transfer.Arguments.Count)
        {
            diagnostics.Add(Diagnostic(
                "ssa.block-argument.count",
                $"Edge '{sourceBlockId}' -> '{target.Id}' in function '{function.Id}' passes {transfer.Arguments.Count} arguments but target expects {target.Parameters.Count}."));
            return;
        }

        for (var index = 0; index < transfer.Arguments.Count; index++)
        {
            if (!state.Values.TryGetValue(transfer.Arguments[index], out var argumentDefinition))
                continue;

            var expectedType = target.Parameters[index].Value.Type;
            if (argumentDefinition.Value.Type != expectedType)
            {
                diagnostics.Add(Diagnostic(
                    "ssa.block-argument.type",
                    $"Edge '{sourceBlockId}' -> '{target.Id}' argument {index} expects type '{expectedType}' but value '{transfer.Arguments[index]}' has type '{argumentDefinition.Value.Type}'."));
            }
        }
    }

    private static void VerifyUses(
        SsaFunction function,
        IReadOnlyDictionary<SsaBlockId, SsaBlock> blocks,
        DefinitionState state,
        IReadOnlySet<SsaBlockId> reachable,
        IReadOnlyDictionary<SsaBlockId, HashSet<SsaBlockId>> dominators,
        List<IrDiagnostic> diagnostics)
    {
        foreach (var block in function.Blocks)
        {
            for (var index = 0; index < block.Instructions.Count; index++)
            {
                foreach (var operand in block.Instructions[index].Operands)
                    VerifyUse(function, block.Id, index, operand, state, reachable, dominators, diagnostics);
            }

            if (block.Terminator is null)
                continue;

            foreach (var operand in block.Terminator.Operands)
                VerifyUse(function, block.Id, block.Instructions.Count, operand, state, reachable, dominators, diagnostics);

            foreach (var argument in block.Terminator.Transfers.SelectMany(static x => x.Arguments))
                VerifyUse(function, block.Id, block.Instructions.Count, argument, state, reachable, dominators, diagnostics);
        }

        foreach (var blockId in blocks.Keys)
        {
            if (!reachable.Contains(blockId))
            {
                diagnostics.Add(Diagnostic(
                    "ssa.block.unreachable",
                    $"Function '{function.Id}' contains unreachable block '{blockId}'."));
            }
        }
    }

    private static void VerifyUse(
        SsaFunction function,
        SsaBlockId userBlockId,
        int userInstructionIndex,
        SsaValueId usedValueId,
        DefinitionState state,
        IReadOnlySet<SsaBlockId> reachable,
        IReadOnlyDictionary<SsaBlockId, HashSet<SsaBlockId>> dominators,
        List<IrDiagnostic> diagnostics)
    {
        if (!state.Values.TryGetValue(usedValueId, out var definition))
        {
            diagnostics.Add(Diagnostic(
                "ssa.value.undefined",
                $"Function '{function.Id}' uses undefined SSA value '{usedValueId}'."));
            return;
        }

        if (!reachable.Contains(userBlockId))
            return;

        if (definition.BlockId == userBlockId)
        {
            if (definition.InstructionIndex >= userInstructionIndex && definition.InstructionIndex >= 0)
            {
                diagnostics.Add(Diagnostic(
                    "ssa.value.use-before-definition",
                    $"Function '{function.Id}' uses value '{usedValueId}' before its definition in block '{userBlockId}'."));
            }

            return;
        }

        if (!dominators.TryGetValue(userBlockId, out var userDominators) || !userDominators.Contains(definition.BlockId))
        {
            diagnostics.Add(Diagnostic(
                "ssa.value.dominance",
                $"Value '{usedValueId}' defined in block '{definition.BlockId}' does not dominate its use in block '{userBlockId}' in function '{function.Id}'."));
        }
    }

    private static HashSet<SsaBlockId> ComputeReachable(
        SsaBlockId entryBlockId,
        IReadOnlyDictionary<SsaBlockId, SsaBlock> blocks)
    {
        var reachable = new HashSet<SsaBlockId>();
        var pending = new Stack<SsaBlockId>();
        pending.Push(entryBlockId);

        while (pending.Count > 0)
        {
            var blockId = pending.Pop();
            if (!blocks.TryGetValue(blockId, out var block) || !reachable.Add(blockId) || block.Terminator is null)
                continue;

            foreach (var target in block.Terminator.Transfers.Select(static x => x.Target).Order())
                pending.Push(target);
        }

        return reachable;
    }

    private static Dictionary<SsaBlockId, HashSet<SsaBlockId>> ComputeDominators(
        SsaBlockId entryBlockId,
        IReadOnlyDictionary<SsaBlockId, SsaBlock> blocks,
        IReadOnlySet<SsaBlockId> reachable)
    {
        var predecessors = blocks.Keys.ToDictionary(static x => x, static _ => new HashSet<SsaBlockId>());
        foreach (var block in blocks.Values)
        {
            if (block.Terminator is null)
                continue;

            foreach (var target in block.Terminator.Transfers.Select(static x => x.Target))
            {
                if (predecessors.TryGetValue(target, out var targetPredecessors))
                    targetPredecessors.Add(block.Id);
            }
        }

        var reachableBlocks = reachable.Order().ToList();
        var dominators = new Dictionary<SsaBlockId, HashSet<SsaBlockId>>();
        foreach (var blockId in reachableBlocks)
        {
            dominators[blockId] = blockId == entryBlockId
                ? [entryBlockId]
                : reachableBlocks.ToHashSet();
        }

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var blockId in reachableBlocks.Where(x => x != entryBlockId))
            {
                var predecessorSets = predecessors[blockId]
                    .Where(reachable.Contains)
                    .Select(predecessor => dominators[predecessor])
                    .ToList();

                var next = predecessorSets.Count == 0
                    ? new HashSet<SsaBlockId>()
                    : predecessorSets
                        .Skip(1)
                        .Aggregate(
                            new HashSet<SsaBlockId>(predecessorSets[0]),
                            static (current, nextSet) =>
                            {
                                current.IntersectWith(nextSet);
                                return current;
                            });

                next.Add(blockId);
                if (!dominators[blockId].SetEquals(next))
                {
                    dominators[blockId] = next;
                    changed = true;
                }
            }
        }

        return dominators;
    }

    private static IrVerificationResult Error(string code, string message) => new([Diagnostic(code, message)]);

    private static IrDiagnostic Diagnostic(string code, string message) => new(IrDiagnosticSeverity.Error, code, message);

    private sealed class DefinitionState
    {
        public Dictionary<SsaValueId, ValueDefinition> Values { get; } = new();

        public HashSet<SsaOperationId> InstructionIds { get; } = [];
    }

    private sealed record ValueDefinition(SsaValue Value, SsaBlockId BlockId, int InstructionIndex);
}
