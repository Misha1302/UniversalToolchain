using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Capabilities;

namespace ConditionsModule.Optimizers;

[DialectOptimizerAlias("ComparisonIntrinsicOptimization")]
[DialectRuntimeExport("Optimizer", "ComparisonIntrinsicOptimization")]
[AutoRegisterService]
[IntrinsicDescriptorProvider(typeof(ComparisonIntrinsicDescriptorProvider))]
[UsedImplicitly]
public class ComparisonIntrinsicOptimizerModule : IIRProcessingModule
{
    private static readonly IReadOnlyList<Type> _supportedComparisonTypes =
    [
        typeof(int), typeof(long), typeof(float), typeof(double)
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

    private IOptimizerIntrinsicCapabilityContext? _capabilityContext;

    public void InitIntrinsicCapabilityContext(IOptimizerIntrinsicCapabilityContext capabilityContext)
    {
        _capabilityContext = capabilityContext ?? throw new ArgumentNullException(nameof(capabilityContext));
    }

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        var capabilityContext = _capabilityContext
                                ?? throw new InvalidOperationException("Comparison optimizer requires intrinsic capability context initialization.");

        if (!HasRequiredCapabilities(capabilityContext))
            return current;

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

    private static bool HasRequiredCapabilities(IOptimizerIntrinsicCapabilityContext capabilityContext)
    {
        foreach (var type in _supportedComparisonTypes)
        {
            if (!capabilityContext.Supports(BuiltinIntrinsicSymbols.Comparison.Equal, type) ||
                !capabilityContext.Supports(BuiltinIntrinsicSymbols.Comparison.NotEqual, type) ||
                !capabilityContext.Supports(BuiltinIntrinsicSymbols.Comparison.Greater, type) ||
                !capabilityContext.Supports(BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual, type) ||
                !capabilityContext.Supports(BuiltinIntrinsicSymbols.Comparison.Less, type) ||
                !capabilityContext.Supports(BuiltinIntrinsicSymbols.Comparison.LessOrEqual, type))
                return false;
        }

        return true;
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
            if (instruction.Operands.Count == 0 || instruction.Operands[0] is not string intrinsicName)
                return;

            if (intrinsicName == "call C#")
            {
                AirTypes.ProcessTypesIntrinsic(instruction, stack);
                return;
            }

            if (intrinsicName.StartsWith("cmp_", StringComparison.Ordinal))
            {
                stack.Pop();
                stack.Pop();
                stack.Push(typeof(bool));
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
