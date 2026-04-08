using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Intrinsics.Builtins;

namespace NativeMathModule;

[DialectOptimizerAlias("ArithmeticOptimization")]
[DialectRuntimeExport("Optimizer", "ArithmeticOptimization")]
[AutoRegisterService]
[IntrinsicDescriptorProvider(typeof(ArithmeticIntrinsicDescriptorProvider))]
[ArithmeticModeCompatibility(ArithmeticMode.Native)]
[UsedImplicitly]
public class ArithmeticOptimizerModule : IIRProcessingModule
{
    // Lightweight e-graph-inspired symbolic simplifier for straight-line postfix IR.
    // This is intentionally not a full equality-saturation e-graph engine.
    private static readonly IReadOnlyList<string> _standardModuleIntrinsics =
    [
        "add_i32", "sub_i32", "mul_i32", "div_i32",
        "add_i64", "sub_i64", "mul_i64", "div_i64",
        "add_f32", "sub_f32", "mul_f32", "div_f32",
        "add_f64", "sub_f64", "mul_f64", "div_f64"
    ];

    private static readonly IReadOnlyList<string> _decimalModuleIntrinsics =
    [
        "add_decimal", "sub_decimal", "mul_decimal", "div_decimal"
    ];

    private bool _isDecimalsSupported;

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        if (_standardModuleIntrinsics.Any(x => !compiler.SupportedIntrinsics.Contains(x)))
            return current;
        _isDecimalsSupported = _decimalModuleIntrinsics.All(x => compiler.SupportedIntrinsics.Contains(x));

        current = OptimizeArithmetic(current);
        return current;
    }

    private IAbstractIR OptimizeArithmetic(IAbstractIR air)
    {
        var instructions = air.Instructions.ToList();
        var context = new CompilationContext();

        for (var i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];

            if (instruction.UOpCode == UOpCode.Intrinsic &&
                instruction.Operands.Count >= 2 &&
                (string)instruction.Operands[0] == "call C#")
            {
                var method = instruction.Operands[1].Get<MethodInfo>();

                if (method.DeclaringType == typeof(NativeArithmetic))
                {
                    var intrinsicName = GetIntrinsicName(method);
                    if (intrinsicName != null)
                    {
                        context.NewInstructions.Add(new Instruction(UOpCode.Intrinsic, [intrinsicName]));
                        continue;
                    }
                }
            }

            context.NewInstructions.Add(instruction);
        }

        var optimizedInstructions = ApplyArithmeticPeepholes(context.NewInstructions);

        var result = new AbstractIR();
        result.AppendInstructions(optimizedInstructions);
        return result;
    }

    private static IReadOnlyList<Instruction> ApplyArithmeticPeepholes(IReadOnlyList<Instruction> instructions)
    {
        var optimized = instructions.ToList();
        var changed = true;

        while (changed)
        {
            changed = false;
            for (var i = 0; i < optimized.Count; i++)
            {
                if (TryFoldAddMulReassociation(optimized, i))
                {
                    changed = true;
                    break;
                }

                if (TrySimplifyIdentity(optimized, i))
                {
                    changed = true;
                    break;
                }

                if (TryCanonicalizeCommutativeOperands(optimized, i))
                {
                    changed = true;
                    break;
                }
            }
        }

        return optimized;
    }

    private static bool TrySimplifyIdentity(List<Instruction> instructions, int index)
    {
        if (index + 2 >= instructions.Count)
            return false;

        var left = instructions[index];
        var right = instructions[index + 1];
        var op = instructions[index + 2];

        if (!TryGetArithmeticIntrinsic(op, out var name, out var suffix))
            return false;

        if (name == "sub" && IsSingleValueProducer(left) && TryGetNumericConstant(right, out var rightValue) && IsZero(rightValue))
            return Replace3With1(instructions, index, left);

        if (name == "div" && IsSingleValueProducer(left) && TryGetNumericConstant(right, out rightValue) && IsOne(rightValue))
            return Replace3With1(instructions, index, left);

        if (name == "add")
        {
            if (TryGetNumericConstant(left, out var leftValue) && IsZero(leftValue) && IsSingleValueProducer(right))
                return Replace3With1(instructions, index, right);

            if (TryGetNumericConstant(right, out rightValue) && IsZero(rightValue) && IsSingleValueProducer(left))
                return Replace3With1(instructions, index, left);
        }

        if (name == "mul")
        {
            if (TryGetNumericConstant(left, out var leftValue))
            {
                if (IsOne(leftValue) && IsSingleValueProducer(right))
                    return Replace3With1(instructions, index, right);
                if (IsIntegerSuffix(suffix) && IsZero(leftValue))
                    return Replace3With1(instructions, index, new Instruction(UOpCode.Push, [GetTypedZero(suffix)]));
            }

            if (TryGetNumericConstant(right, out rightValue))
            {
                if (IsOne(rightValue) && IsSingleValueProducer(left))
                    return Replace3With1(instructions, index, left);
                if (IsIntegerSuffix(suffix) && IsZero(rightValue))
                    return Replace3With1(instructions, index, new Instruction(UOpCode.Push, [GetTypedZero(suffix)]));
            }
        }

        return false;
    }

    private static bool TryFoldAddMulReassociation(List<Instruction> instructions, int index)
    {
        if (index + 4 >= instructions.Count)
            return false;

        var x = instructions[index];
        var c1 = instructions[index + 1];
        var op1 = instructions[index + 2];
        var c2 = instructions[index + 3];
        var op2 = instructions[index + 4];

        if (!IsSingleValueProducer(x) ||
            !TryGetArithmeticIntrinsic(op1, out var name1, out var suffix1) ||
            !TryGetArithmeticIntrinsic(op2, out var name2, out var suffix2) ||
            name1 != name2 ||
            suffix1 != suffix2 ||
            !IsIntegerSuffix(suffix1) ||
            !TryGetNumericConstant(c1, out var v1) ||
            !TryGetNumericConstant(c2, out var v2))
            return false;

        object? combined = null;
        if (name1 == "add")
            combined = CombineIntegerConstants(v1, v2, false, suffix1);
        else if (name1 == "mul")
            combined = CombineIntegerConstants(v1, v2, true, suffix1);

        if (combined is null)
            return false;

        instructions[index + 1] = new Instruction(UOpCode.Push, [combined]);
        instructions.RemoveAt(index + 3);
        instructions.RemoveAt(index + 3);
        return true;
    }

    private static bool TryCanonicalizeCommutativeOperands(List<Instruction> instructions, int index)
    {
        if (index + 2 >= instructions.Count)
            return false;

        var left = instructions[index];
        var right = instructions[index + 1];
        var op = instructions[index + 2];
        if (!TryGetArithmeticIntrinsic(op, out var name, out _) || name != "add" && name != "mul")
            return false;

        if (!TryGetNumericConstant(left, out _) || TryGetNumericConstant(right, out _) || !IsSingleValueProducer(right))
            return false;

        instructions[index] = right;
        instructions[index + 1] = left;
        return true;
    }


    private static bool IsSingleValueProducer(Instruction instruction)
    {
        if (instruction.UOpCode == UOpCode.Push)
            return true;

        return instruction.UOpCode == UOpCode.Intrinsic &&
               instruction.Operands.Count > 0 &&
               instruction.Operands[0] is string intrinsicName &&
               (intrinsicName == "load_local" || intrinsicName == "load_local_ref" || intrinsicName.StartsWith("ldloc", StringComparison.Ordinal));
    }

    private static bool Replace3With1(List<Instruction> instructions, int index, Instruction replacement)
    {
        instructions[index] = replacement;
        instructions.RemoveAt(index + 1);
        instructions.RemoveAt(index + 1);
        return true;
    }

    private static bool TryGetArithmeticIntrinsic(Instruction instruction, out string operation, out string suffix)
    {
        operation = string.Empty;
        suffix = string.Empty;
        if (instruction.UOpCode != UOpCode.Intrinsic || instruction.Operands.Count == 0 || instruction.Operands[0] is not string name)
            return false;

        var split = name.Split('_');
        if (split.Length != 2)
            return false;

        operation = split[0];
        suffix = split[1];
        return operation is "add" or "sub" or "mul" or "div";
    }

    private static bool TryGetNumericConstant(Instruction instruction, out object value)
    {
        value = null!;
        if (instruction.UOpCode != UOpCode.Push || instruction.Operands.Count != 1)
            return false;

        value = instruction.Operands[0];
        return value is int or long or float or double or decimal;
    }

    private static bool IsIntegerSuffix(string suffix) => suffix is "i32" or "i64";

    private static bool IsZero(object value) => value switch
    {
        int i => i == 0,
        long l => l == 0,
        float f => f == 0f,
        double d => d == 0d,
        decimal m => m == 0m,
        _ => false
    };

    private static bool IsOne(object value) => value switch
    {
        int i => i == 1,
        long l => l == 1,
        float f => Math.Abs(f - 1f) < 1e-9,
        double d => Math.Abs(d - 1d) < 1e-9,
        decimal m => m == 1m,
        _ => false
    };

    private static object GetTypedZero(string suffix) => suffix switch
    {
        "i32" => 0,
        "i64" => 0L,
        _ => 0
    };

    private static object? CombineIntegerConstants(object left, object right, bool isMultiply, string suffix)
    {
        if (suffix == "i32" && left is int li && right is int ri)
            return isMultiply ? li * ri : li + ri;
        if (suffix == "i64" && left is long ll && right is long rl)
            return isMultiply ? ll * rl : ll + rl;
        return null;
    }

    private string? GetIntrinsicName(MethodInfo method)
    {
        var typeMap = new Dictionary<Type, string>
        {
            [typeof(int)] = "i32",
            [typeof(long)] = "i64",
            [typeof(float)] = "f32",
            [typeof(double)] = "f64",
            [typeof(decimal)] = "decimal"
        };

        var opMap = new Dictionary<string, string>
        {
            ["Add"] = "add",
            ["Subtract"] = "sub",
            ["Multiply"] = "mul",
            ["Divide"] = "div"
        };

        string? typeSuffix = null;
        string? operation = null;

        // Обработка обобщенных методов (int, long, float, double)
        if (method.IsGenericMethod)
        {
            var genericType = method.GetGenericArguments()[0];
            if (typeMap.TryGetValue(genericType, out var resolvedTypeSuffix))
            {
                typeSuffix = resolvedTypeSuffix;
                operation = opMap.GetValueOrDefault(method.Name);
            }
        }
        // Обработка методов для decimal
        else if (method.Name.EndsWith("Decimal"))
        {
            typeSuffix = "decimal";
            operation = opMap.GetValueOrDefault(method.Name.Replace("Decimal", ""));
        }

        return operation is not null && typeSuffix is not null ? $"{operation}_{typeSuffix}" : null;
    }

    private class CompilationContext
    {
        public List<Instruction> NewInstructions { get; } = new();
    }
}
