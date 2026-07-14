using BasicCore.Builtins;
using BasicCore.Capabilities;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace ConditionsModule.Optimizers;

[DialectOptimizerAlias("BooleanOptimization")]
[DialectRuntimeExport("Optimizer", "BooleanOptimization")]
[AutoRegisterService]
[IntrinsicDescriptorProvider(typeof(BooleanIntrinsicDescriptorProvider))]
[UsedImplicitly]
public class BooleanOptimizerModule : IAirOptimizer
{
    private IOptimizerIntrinsicCapabilityContext? _capabilityContext;

    public void InitIntrinsicCapabilityContext(IOptimizerIntrinsicCapabilityContext capabilityContext)
    {
        _capabilityContext = capabilityContext.NotNull("Argument 'capabilityContext' cannot be null.");
    }

    public IAbstractIR Optimize(IAbstractIR current)
    {
        var capabilityContext = _capabilityContext.NotNull(
            "Boolean optimizer requires intrinsic capability context initialization.");

        if (!OptimizerCapabilityGuards.SupportsAll(
                capabilityContext,
                (BuiltinIntrinsicSymbols.Boolean.And, []),
                (BuiltinIntrinsicSymbols.Boolean.Or, []),
                (BuiltinIntrinsicSymbols.Boolean.Not, [])))
            return current;

        var optimized = OptimizeNativeLoads(current);
        return OptimizeBooleanPeepholes(optimized);
    }

    private IAbstractIR OptimizeNativeLoads(IAbstractIR air)
    {
        var instructions = air.Instructions.ToList();
        var context = new CompilationContext();

        var methodToIntrinsic = new Dictionary<string, IntrinsicSymbol>
        {
            [nameof(BooleanVisitor.BooleanOperations.And)] = BuiltinIntrinsicSymbols.Boolean.And,
            [nameof(BooleanVisitor.BooleanOperations.Or)] = BuiltinIntrinsicSymbols.Boolean.Or,
            [nameof(BooleanVisitor.BooleanOperations.Not)] = BuiltinIntrinsicSymbols.Boolean.Not
        };

        for (var i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];

            if (CSharpCallIntrinsicReader.TryGetCallMethod(instruction, out var method)
                && method.DeclaringType == typeof(BooleanVisitor.BooleanOperations)
                && methodToIntrinsic.TryGetValue(method.Name, out var mappedIntrinsicSymbol))
            {
                context.NewInstructions.Add(BuiltinIntrinsicInstruction.Create(mappedIntrinsicSymbol));
                continue;
            }

            context.NewInstructions.Add(instruction);
        }

        var result = new AbstractIR();
        result.AppendInstructions(context.NewInstructions);
        return result;
    }

    private static IAbstractIR OptimizeBooleanPeepholes(IAbstractIR air)
    {
        var instructions = air.Instructions.ToList();
        var optimized = new List<Instruction>();

        for (var i = 0; i < instructions.Count; i++)
        {
            if (TryFoldBooleanNot(instructions, i, out var foldedNot))
            {
                optimized.Add(foldedNot);
                i += 1;
                continue;
            }

            if (TryFoldBooleanBinaryLiterals(instructions, i, out var foldedBinary))
            {
                optimized.Add(foldedBinary);
                i += 2;
                continue;
            }

            if (TryApplyIdentityLaw(instructions, i, out var replacement))
            {
                optimized.Add(replacement);
                i += 2;
                continue;
            }

            optimized.Add(instructions[i]);
        }

        var result = new AbstractIR();
        result.AppendInstructions(optimized);
        return result;
    }

    private static bool TryFoldBooleanNot(IReadOnlyList<Instruction> instructions, int start, out Instruction folded)
    {
        folded = null!;
        if (start + 1 >= instructions.Count)
            return false;

        if (!TryGetBoolPush(instructions[start], out var value) ||
            !IsBooleanIntrinsic(instructions[start + 1], BuiltinIntrinsicSymbols.Boolean.Not))
            return false;

        folded = new Instruction(UOpCode.Push, [!value]);
        return true;
    }

    private static bool TryFoldBooleanBinaryLiterals(IReadOnlyList<Instruction> instructions, int start, out Instruction folded)
    {
        folded = null!;
        if (start + 2 >= instructions.Count)
            return false;

        if (!TryGetBoolPush(instructions[start], out var left) || !TryGetBoolPush(instructions[start + 1], out var right))
            return false;

        if (IsBooleanIntrinsic(instructions[start + 2], BuiltinIntrinsicSymbols.Boolean.And))
        {
            folded = new Instruction(UOpCode.Push, [left && right]);
            return true;
        }

        if (IsBooleanIntrinsic(instructions[start + 2], BuiltinIntrinsicSymbols.Boolean.Or))
        {
            folded = new Instruction(UOpCode.Push, [left || right]);
            return true;
        }

        return false;
    }

    private static bool TryApplyIdentityLaw(IReadOnlyList<Instruction> instructions, int start, out Instruction replacement)
    {
        replacement = null!;
        if (start + 2 >= instructions.Count)
            return false;

        if (IsBooleanIntrinsic(instructions[start + 2], BuiltinIntrinsicSymbols.Boolean.And) &&
            TryGetBoolPush(instructions[start + 1], out var andRight) &&
            andRight)
        {
            replacement = instructions[start];
            return true;
        }

        if (IsBooleanIntrinsic(instructions[start + 2], BuiltinIntrinsicSymbols.Boolean.Or) &&
            TryGetBoolPush(instructions[start + 1], out var orRight) &&
            !orRight)
        {
            replacement = instructions[start];
            return true;
        }

        return false;
    }

    private static bool IsBooleanIntrinsic(Instruction instruction, IntrinsicSymbol symbol) =>
        BuiltinIntrinsicInstruction.Is(instruction, symbol);

    private static bool TryGetBoolPush(Instruction instruction, out bool value)
    {
        value = false;
        if (instruction.UOpCode != UOpCode.Push || instruction.Operands.Count != 1 || instruction.Operands[0] is not bool boolValue)
            return false;

        value = boolValue;
        return true;
    }

    private class CompilationContext
    {
        public List<Instruction> NewInstructions { get; } =
        [
        ];
    }
}