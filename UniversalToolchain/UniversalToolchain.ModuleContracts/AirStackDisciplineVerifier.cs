using BasicCore.Core;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Air.Analysis;

namespace UniversalToolchain.ModuleContracts;

internal sealed class AirStackDisciplineVerifier
{
    private readonly AirControlFlowGraphBuilder _graphBuilder;
    private readonly IInstructionIntrinsicReader _intrinsicReader;
    private readonly IIntrinsicTypeStackProcessor _intrinsicTypeStackProcessor;

    public AirStackDisciplineVerifier(
        IInstructionIntrinsicReader intrinsicReader,
        IIntrinsicTypeStackProcessor intrinsicTypeStackProcessor)
        : this(new AirControlFlowGraphBuilder(), intrinsicReader, intrinsicTypeStackProcessor)
    {
    }

    internal AirStackDisciplineVerifier(
        AirControlFlowGraphBuilder graphBuilder,
        IInstructionIntrinsicReader intrinsicReader,
        IIntrinsicTypeStackProcessor intrinsicTypeStackProcessor)
    {
        _graphBuilder = graphBuilder.ArgNotNull();
        _intrinsicReader = intrinsicReader.ArgNotNull();
        _intrinsicTypeStackProcessor = intrinsicTypeStackProcessor.ArgNotNull();
    }

    public IReadOnlyList<ToolchainDiagnostic> Verify(
        IAbstractIR air,
        ToolchainDiagnosticSeverity severity)
    {
        air = air.ArgNotNull();

        var graphResult = _graphBuilder.Build(air.Instructions);
        if (graphResult.Diagnostics.Count != 0)
            return [];

        var diagnostics = new List<ToolchainDiagnostic>();
        var entryStates = new Dictionary<AirBlockId, IReadOnlyList<Type>>
        {
            [graphResult.Graph.EntryBlockId] = []
        };
        var terminalStates = new Dictionary<AirBlockId, IReadOnlyList<Type>>();
        var pending = new Queue<AirBlockId>();
        pending.Enqueue(graphResult.Graph.EntryBlockId);

        while (pending.Count > 0)
        {
            var blockId = pending.Dequeue();
            if (!graphResult.Graph.BlocksById.TryGetValue(blockId, out var block) ||
                !entryStates.TryGetValue(blockId, out var entryState))
            {
                continue;
            }

            var simulation = Simulate(block, entryState, severity, diagnostics);
            if (block.Terminator.Successors.Count == 0 && simulation.IsValid)
                terminalStates[blockId] = simulation.ExitState;

            foreach (var edge in block.Terminator.Successors
                         .OrderBy(static x => x.Target)
                         .ThenBy(static x => x.Kind))
            {
                if (!entryStates.TryGetValue(edge.Target, out var current))
                {
                    entryStates[edge.Target] = simulation.ExitState;
                    pending.Enqueue(edge.Target);
                    continue;
                }

                if (current.SequenceEqual(simulation.ExitState))
                    continue;

                diagnostics.Add(CreateDiagnostic(
                    severity,
                    $"Incompatible AIR stack state at edge '{edge.Source}' -> '{edge.Target}': " +
                    $"current {Format(current)}, incoming {Format(simulation.ExitState)}."));
            }
        }

        VerifyTerminalStates(terminalStates, severity, diagnostics);

        return diagnostics
            .DistinctBy(static x => (x.Code, x.Message))
            .OrderBy(static x => x.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private SimulationResult Simulate(
        AirBasicBlock block,
        IReadOnlyList<Type> entryState,
        ToolchainDiagnosticSeverity severity,
        List<ToolchainDiagnostic> diagnostics)
    {
        var stack = entryState.ToList();
        var isValid = true;

        for (var offset = 0; offset < block.Instructions.Count; offset++)
        {
            var instruction = block.Instructions[offset];
            var instructionIndex = block.StartInstructionIndex + offset;
            var diagnosticsBefore = diagnostics.Count;

            try
            {
                SimulateInstruction(instruction, stack, instructionIndex, block.Id, severity, diagnostics);
            }
            catch (Exception exception)
            {
                diagnostics.Add(CreateDiagnostic(
                    severity,
                    $"AIR instruction {instructionIndex} in block '{block.Id}' violates stack discipline: {exception.Message}"));
            }

            if (diagnostics.Count != diagnosticsBefore)
                isValid = false;
        }

        return new SimulationResult(stack.ToArray(), isValid);
    }

    private void SimulateInstruction(
        Instruction instruction,
        List<Type> stack,
        int instructionIndex,
        AirBlockId blockId,
        ToolchainDiagnosticSeverity severity,
        List<ToolchainDiagnostic> diagnostics)
    {
        switch (instruction.UOpCode)
        {
            case UOpCode.Nop:
            case UOpCode.Label:
            case UOpCode.Annotate:
            case UOpCode.Jmp:
                return;
            case UOpCode.Push:
                stack.Add(GetPushedType(instruction));
                return;
            case UOpCode.Drop:
                PopRequired(stack, instructionIndex, blockId, instruction.UOpCode, severity, diagnostics);
                return;
            case UOpCode.JmpIf:
            case UOpCode.JmpIfNot:
                var conditionType = PopRequired(
                    stack,
                    instructionIndex,
                    blockId,
                    instruction.UOpCode,
                    severity,
                    diagnostics);
                if (conditionType != null && conditionType != typeof(bool))
                {
                    diagnostics.Add(CreateDiagnostic(
                        severity,
                        $"AIR conditional jump at instruction {instructionIndex} in block '{blockId}' " +
                        $"expects '{typeof(bool)}' but consumes '{conditionType}'."));
                }

                return;
            case UOpCode.Intrinsic:
                if (!_intrinsicReader.TryRead(instruction, out var invocation))
                    throw new InvalidOperationException("Intrinsic instruction does not contain a canonical typed invocation.");

                _intrinsicTypeStackProcessor.Process(invocation, stack);
                return;
            default:
                throw new InvalidOperationException($"Unknown AIR opcode '{instruction.UOpCode}'.");
        }
    }

    private static Type GetPushedType(Instruction instruction)
    {
        var operand = instruction.Operands[0];
        return operand switch
        {
            AirExternalValueReference external => external.ValueType,
            _ => AirPushOperand.GetDeclaredType(operand)
        };
    }

    private static Type? PopRequired(
        List<Type> stack,
        int instructionIndex,
        AirBlockId blockId,
        UOpCode opcode,
        ToolchainDiagnosticSeverity severity,
        List<ToolchainDiagnostic> diagnostics)
    {
        if (stack.Count == 0)
        {
            diagnostics.Add(CreateDiagnostic(
                severity,
                $"AIR stack underflow at instruction {instructionIndex} in block '{blockId}' for opcode '{opcode}'."));
            return null;
        }

        var result = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return result;
    }

    private static void VerifyTerminalStates(
        IReadOnlyDictionary<AirBlockId, IReadOnlyList<Type>> terminalStates,
        ToolchainDiagnosticSeverity severity,
        List<ToolchainDiagnostic> diagnostics)
    {
        IReadOnlyList<Type>? canonical = null;
        AirBlockId canonicalBlock = default;

        foreach (var (blockId, stack) in terminalStates.OrderBy(static x => x.Key))
        {
            if (stack.Count > 1)
            {
                diagnostics.Add(CreateDiagnostic(
                    severity,
                    $"AIR terminal block '{blockId}' finishes with {stack.Count} evaluation-stack values {Format(stack)}; expected zero or one."));
                continue;
            }

            if (canonical is null)
            {
                canonical = stack;
                canonicalBlock = blockId;
                continue;
            }

            if (!canonical.SequenceEqual(stack))
            {
                diagnostics.Add(CreateDiagnostic(
                    severity,
                    $"AIR terminal blocks '{canonicalBlock}' and '{blockId}' expose incompatible return-stack shapes: " +
                    $"{Format(canonical)} versus {Format(stack)}."));
            }
        }
    }

    private static ToolchainDiagnostic CreateDiagnostic(
        ToolchainDiagnosticSeverity severity,
        string message) =>
        new(
            ModuleContractDiagnosticCodes.InvalidAirStackDiscipline,
            severity,
            message,
            null,
            [new ToolchainDiagnosticHint("Repair AIR stack effects before backend execution.")]);

    private static string Format(IReadOnlyList<Type> types) =>
        "[" + string.Join(", ", types.Select(static x => x.FullName ?? x.Name)) + "]";

    private sealed record SimulationResult(IReadOnlyList<Type> ExitState, bool IsValid);
}
