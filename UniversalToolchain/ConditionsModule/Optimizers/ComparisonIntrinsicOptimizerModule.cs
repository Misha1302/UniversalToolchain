using System.Reflection;
using BasicCore.Attributes;
using BasicCore.Contracts;
using ConditionsModule.Enums;
using DotnetAirHelper;
using IntermediateRepresentationAbstractions;
using JetBrains.Annotations;
using ListExtensions;
using ObjectExtensions;
using UniversalIntermediateRepresentation;

namespace ConditionsModule.Optimizers;

[AutoRegisterService]
[UsedImplicitly]
public class ComparisonIntrinsicOptimizerModule : IIRProcessingModule
{
    private static readonly IReadOnlyList<string> _comparisonIntrinsics =
    [
        "cmp_eq_i32", "cmp_ne_i32", "cmp_gt_i32", "cmp_ge_i32", "cmp_lt_i32", "cmp_le_i32",
        "cmp_eq_i64", "cmp_ne_i64", "cmp_gt_i64", "cmp_ge_i64", "cmp_lt_i64", "cmp_le_i64",
        "cmp_eq_f32", "cmp_ne_f32", "cmp_gt_f32", "cmp_ge_f32", "cmp_lt_f32", "cmp_le_f32",
        "cmp_eq_f64", "cmp_ne_f64", "cmp_gt_f64", "cmp_ge_f64", "cmp_lt_f64", "cmp_le_f64"
    ];

    private static readonly IReadOnlyDictionary<string, string> _comparisonOperations = new Dictionary<string, string>
    {
        [nameof(Comparisons.Equal)] = "eq",
        [nameof(Comparisons.NotEqual)] = "ne",
        [nameof(Comparisons.Greater)] = "gt",
        [nameof(Comparisons.GreaterOrEqual)] = "ge",
        [nameof(Comparisons.Less)] = "lt",
        [nameof(Comparisons.LessOrEqual)] = "le"
    };

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        if (_comparisonIntrinsics.Any(x => !compiler.SupportedIntrinsics.Contains(x)))
            return current;

        InitializeAirTypes();

        var result = new AbstractIR();
        var optimized = new List<Instruction>();
        var stack = new List<Type>();

        foreach (var instruction in current.Instructions)
        {
            if (TryBuildComparisonIntrinsic(instruction, stack, out var intrinsic))
            {
                var optimizedInstruction = new Instruction(UOpCode.Intrinsic, [intrinsic]);
                optimized.Add(optimizedInstruction);
                ApplyInstructionTypes(optimizedInstruction, stack);
                continue;
            }

            optimized.Add(instruction);
            ApplyInstructionTypes(instruction, stack);
        }

        result.AppendInstructions(optimized);
        return result;
    }


    private static void ApplyInstructionTypes(Instruction instruction, List<Type> stack)
    {
        if (instruction.UOpCode == UOpCode.Push)
        {
            stack.Push(instruction.Operands[0].GetType());
            return;
        }

        if (instruction.UOpCode == UOpCode.Drop)
        {
            stack.Pop();
            return;
        }

        if (instruction.UOpCode == UOpCode.Intrinsic)
        {
            AirTypes.ProcessTypesIntrinsic(instruction, stack);
            return;
        }
    }

    private static void InitializeAirTypes()
    {
        foreach (var type in new[] { "i32", "i64", "f32", "f64" })
        {
            foreach (var op in new[] { "eq", "ne", "gt", "ge", "lt", "le" })
            {
                AirTypes.TryRegisterIntrinsic($"cmp_{op}_{type}", (_, stack) =>
                {
                    stack.Pop();
                    stack.Pop();
                    stack.Push(typeof(bool));
                });
            }
        }
    }

    private static bool TryBuildComparisonIntrinsic(Instruction instruction, IReadOnlyList<Type> stack, out string intrinsic)
    {
        intrinsic = string.Empty;

        if (instruction.UOpCode != UOpCode.Intrinsic ||
            instruction.Operands.Count < 2 ||
            instruction.Operands[0] is not string { } intrinsicName ||
            intrinsicName != "call C#")
            return false;

        var method = instruction.Operands[1].Get<MethodInfo>();
        if (method.DeclaringType != typeof(Comparisons) || !_comparisonOperations.TryGetValue(method.Name, out var operation))
            return false;

        var operandType = ResolveOperandType(method, stack);
        if (!TryMapTypeToSuffix(operandType, out var suffix))
            return false;

        intrinsic = $"cmp_{operation}_{suffix}";
        return true;
    }

    private static Type? ResolveOperandType(MethodInfo method, IReadOnlyList<Type> stack)
    {
        if (stack.Count >= 2 && stack[^1] == stack[^2])
            return stack[^1];

        if (method.IsGenericMethod)
        {
            var genericArgument = method.GetGenericArguments()[0];
            if (!genericArgument.IsGenericParameter)
                return genericArgument;
        }

        return null;
    }

    private static bool TryMapTypeToSuffix(Type? type, out string suffix)
    {
        suffix = string.Empty;
        if (type == typeof(int))
        {
            suffix = "i32";
            return true;
        }

        if (type == typeof(long))
        {
            suffix = "i64";
            return true;
        }

        if (type == typeof(float))
        {
            suffix = "f32";
            return true;
        }

        if (type == typeof(double))
        {
            suffix = "f64";
            return true;
        }

        return false;
    }
}
