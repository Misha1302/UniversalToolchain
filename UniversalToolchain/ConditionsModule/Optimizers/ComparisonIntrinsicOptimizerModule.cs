using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Capabilities;
using UniversalToolchain.Intrinsics.Contracts;

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

    private static readonly IReadOnlyDictionary<string, IntrinsicSymbol> _comparisonOperations = new Dictionary<string, IntrinsicSymbol>
    {
        [nameof(Comparisons.Equal)] = BuiltinIntrinsicSymbols.Comparison.Equal,
        [nameof(Comparisons.NotEqual)] = BuiltinIntrinsicSymbols.Comparison.NotEqual,
        [nameof(Comparisons.Greater)] = BuiltinIntrinsicSymbols.Comparison.Greater,
        [nameof(Comparisons.GreaterOrEqual)] = BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual,
        [nameof(Comparisons.Less)] = BuiltinIntrinsicSymbols.Comparison.Less,
        [nameof(Comparisons.LessOrEqual)] = BuiltinIntrinsicSymbols.Comparison.LessOrEqual
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
                optimized.Add(intrinsic);
                ApplyInstructionTypes(intrinsic, stack);
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
            if (instruction.Operands.Count > 0 &&
                instruction.Operands[0] is string intrinsicName &&
                intrinsicName == "call C#")
            {
                AirTypes.ProcessTypesIntrinsic(instruction, stack);
                return;
            }

            if (BuiltinIntrinsicInstruction.TryGetInvocation(instruction, out var invocation) &&
                IsComparisonSymbol(invocation.Symbol))
            {
                stack.Pop();
                stack.Pop();
                stack.Push(typeof(bool));
            }
        }
    }

    private static bool TryBuildComparisonIntrinsic(Instruction instruction, IReadOnlyList<Type> stack, out Instruction intrinsic)
    {
        intrinsic = null!;

        if (instruction.UOpCode != UOpCode.Intrinsic ||
            instruction.Operands.Count < 2 ||
            instruction.Operands[0] is not string { } intrinsicName ||
            intrinsicName != "call C#")
            return false;

        var method = instruction.Operands[1].Get<MethodInfo>();
        if (method.DeclaringType != typeof(Comparisons) || !_comparisonOperations.TryGetValue(method.Name, out var symbol))
            return false;

        var operandType = ResolveOperandType(method, stack);
        if (operandType is null || !IsSupportedType(operandType))
            return false;

        intrinsic = BuiltinIntrinsicInstruction.Create(symbol, operandType);
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

    private static bool IsSupportedType(Type type)
    {
        return type == typeof(int) ||
               type == typeof(long) ||
               type == typeof(float) ||
               type == typeof(double);
    }

    private static bool IsComparisonSymbol(IntrinsicSymbol symbol)
    {
        return symbol == BuiltinIntrinsicSymbols.Comparison.Equal ||
               symbol == BuiltinIntrinsicSymbols.Comparison.NotEqual ||
               symbol == BuiltinIntrinsicSymbols.Comparison.Greater ||
               symbol == BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual ||
               symbol == BuiltinIntrinsicSymbols.Comparison.Less ||
               symbol == BuiltinIntrinsicSymbols.Comparison.LessOrEqual;
    }
}
