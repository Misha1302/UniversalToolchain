namespace ConditionsModule.Optimizers;

[AutoRegisterService]
[UsedImplicitly]
public class BooleanOptimizerModule : IIRProcessingModule
{
    private static readonly IReadOnlyList<string> _standardModuleIntrinsics =
    [
        "boolean_and", "boolean_or", "boolean_not"
    ];

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        if (_standardModuleIntrinsics.Any(x => !compiler.SupportedIntrinsics.Contains(x)))
            return current;

        InitializeAirTypes();
        var optimized = OptimizeNativeLoads(current);
        return OptimizeBooleanPeepholes(optimized);
    }

    private void InitializeAirTypes()
    {
        AirTypes.TryRegisterIntrinsic(
            "boolean_and",
            (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(typeof(bool));
            }
        );
        AirTypes.TryRegisterIntrinsic(
            "boolean_or",
            (_, stack) =>
            {
                stack.Pop();
                stack.Pop();
                stack.Push(typeof(bool));
            }
        );
        AirTypes.TryRegisterIntrinsic(
            "boolean_not",
            (_, stack) =>
            {
                stack.Pop();
                stack.Push(typeof(bool));
            }
        );
    }

    private IAbstractIR OptimizeNativeLoads(IAbstractIR air)
    {
        var instructions = air.Instructions.ToList();
        var context = new CompilationContext();

        var methodToIntrinsic = new Dictionary<string, string>
        {
            [nameof(BooleanVisitor.BooleanOperations.And)] = "boolean_and",
            [nameof(BooleanVisitor.BooleanOperations.Or)] = "boolean_or",
            [nameof(BooleanVisitor.BooleanOperations.Not)] = "boolean_not"
        };

        for (var i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];

            if (instruction.UOpCode == UOpCode.Intrinsic)
                if (instruction.Operands.Count >= 2 && instruction.Operands[0] is string intrinsicName && intrinsicName == "call C#")
                {
                    var m = instruction.Operands[1].Get<MethodInfo>();

                    if (m.DeclaringType == typeof(BooleanVisitor.BooleanOperations))
                        if (methodToIntrinsic.TryGetValue(m.Name, out var mappedIntrinsicName))
                        {
                            context.NewInstructions.Add(new Instruction(UOpCode.Intrinsic, [mappedIntrinsicName]));
                            continue;
                        }
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

        if (!TryGetBoolPush(instructions[start], out var value) || !IsBooleanIntrinsic(instructions[start + 1], "boolean_not"))
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

        if (IsBooleanIntrinsic(instructions[start + 2], "boolean_and"))
        {
            folded = new Instruction(UOpCode.Push, [left && right]);
            return true;
        }

        if (IsBooleanIntrinsic(instructions[start + 2], "boolean_or"))
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

        if (IsBooleanIntrinsic(instructions[start + 2], "boolean_and") && TryGetBoolPush(instructions[start + 1], out var andRight) && andRight)
        {
            replacement = instructions[start];
            return true;
        }

        if (IsBooleanIntrinsic(instructions[start + 2], "boolean_or") && TryGetBoolPush(instructions[start + 1], out var orRight) && !orRight)
        {
            replacement = instructions[start];
            return true;
        }

        return false;
    }

    private static bool IsBooleanIntrinsic(Instruction instruction, string intrinsicName) =>
        instruction.UOpCode == UOpCode.Intrinsic &&
        instruction.Operands.Count > 0 &&
        instruction.Operands[0] is string name &&
        name == intrinsicName;

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