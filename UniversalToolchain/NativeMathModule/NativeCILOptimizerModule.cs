using System.Collections.Frozen;
using BasicCore.Builtins;
using BasicCore.Capabilities;
using BasicCore.Execution;
using IntermediateRepresentationAbstractions;

namespace NativeMathModule;

[DialectOptimizerAlias("NativeCilOptimization")]
[DialectRuntimeExport("Optimizer", "NativeCilOptimization")]
[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Native)]
public class NativeCilOptimizerModule : IAirOptimizer
{
    // Maps constants to typed load-constant intrinsics for backends that support them.
    private static readonly FrozenDictionary<Type, Action<Instruction, CompilationContext>> _cilGenerators = CreateCilGenerators();

    private static readonly IReadOnlyList<Type> _supportedLoadTypes =
    [
        typeof(int),
        typeof(long),
        typeof(float),
        typeof(double),
        typeof(decimal)
    ];

    private IOptimizerIntrinsicCapabilityContext? _capabilityContext;

    public void InitIntrinsicCapabilityContext(IOptimizerIntrinsicCapabilityContext capabilityContext)
    {
        _capabilityContext = capabilityContext.NotNull("Argument 'capabilityContext' cannot be null.");
    }

    public IAbstractIR Optimize(IAbstractIR current)
    {
        var capabilityContext = _capabilityContext.NotNull(
            "Native CIL optimizer requires intrinsic capability context initialization.");

        var supportsLoadConst = SupportsAllLoadConstTypes(capabilityContext);
        var supportsLoadExternal = SupportsAnyLoadExternalType(capabilityContext);

        if (!supportsLoadConst && !supportsLoadExternal)
            return current;

        return OptimizeNativeLoads(current, supportsLoadConst, supportsLoadExternal, capabilityContext);
    }

    private static bool SupportsAllLoadConstTypes(IOptimizerIntrinsicCapabilityContext capabilityContext)
    {
        var requirements = _supportedLoadTypes
            .Select(type => (BuiltinIntrinsicSymbols.Core.LoadConst, new[] { type }));

        return OptimizerCapabilityGuards.SupportsAll(capabilityContext, requirements);
    }

    private static bool SupportsAnyLoadExternalType(IOptimizerIntrinsicCapabilityContext capabilityContext)
    {
        if (capabilityContext.Supports(BuiltinIntrinsicSymbols.Core.LoadExternal))
            return true;

        return _supportedLoadTypes.Any(type => capabilityContext.Supports(BuiltinIntrinsicSymbols.Core.LoadExternal, type));
    }

    private static FrozenDictionary<Type, Action<Instruction, CompilationContext>> CreateCilGenerators()
    {
        var generators = new Dictionary<Type, Action<Instruction, CompilationContext>>
        {
            [typeof(int)] = (instruction, context) =>
            {
                var value = (int)AirPushOperand.GetValue(instruction.Operands[0])!;
                context.NewInstructions.Add(BuiltinIntrinsicInstruction.Create(
                    BuiltinIntrinsicSymbols.Core.LoadConst,
                    typeof(int),
                    [value]));
            },
            [typeof(long)] = (instruction, context) =>
            {
                var value = (long)AirPushOperand.GetValue(instruction.Operands[0])!;
                context.NewInstructions.Add(BuiltinIntrinsicInstruction.Create(
                    BuiltinIntrinsicSymbols.Core.LoadConst,
                    typeof(long),
                    [value]));
            },
            [typeof(float)] = (instruction, context) =>
            {
                var value = (float)AirPushOperand.GetValue(instruction.Operands[0])!;
                context.NewInstructions.Add(BuiltinIntrinsicInstruction.Create(
                    BuiltinIntrinsicSymbols.Core.LoadConst,
                    typeof(float),
                    [value]));
            },
            [typeof(double)] = (instruction, context) =>
            {
                var value = (double)AirPushOperand.GetValue(instruction.Operands[0])!;
                context.NewInstructions.Add(BuiltinIntrinsicInstruction.Create(
                    BuiltinIntrinsicSymbols.Core.LoadConst,
                    typeof(double),
                    [value]));
            },
            [typeof(decimal)] = (instruction, context) =>
            {
                var value = (decimal)AirPushOperand.GetValue(instruction.Operands[0])!;
                context.NewInstructions.Add(BuiltinIntrinsicInstruction.Create(
                    BuiltinIntrinsicSymbols.Core.LoadConst,
                    typeof(decimal),
                    [value]));
            }
        };

        return generators.ToFrozenDictionary();
    }

    private IAbstractIR OptimizeNativeLoads(IAbstractIR air, bool optimizeLoadConst, bool optimizeLoadExternal, IOptimizerIntrinsicCapabilityContext capabilityContext)
    {
        var instructions = air.Instructions.ToList();
        var context = new CompilationContext();
        var changed = false;

        for (var i = 0; i < instructions.Count; i++)
        {
            if (optimizeLoadExternal && TryOptimizeExternalLoad(instructions, i, context, capabilityContext, out var consumedCount))
            {
                changed = true;
                i += consumedCount - 1;
                continue;
            }

            var instruction = instructions[i];

            if (optimizeLoadConst && instruction.UOpCode == UOpCode.Push && instruction.Operands.Count == 1)
            {
                var value = AirPushOperand.GetValue(instruction.Operands[0]);
                var valueType = value?.GetType();

                if (valueType is not null && _cilGenerators.TryGetValue(valueType, out var generator))
                {
                    changed = true;
                    generator(instruction, context);
                    continue;
                }
            }

            context.NewInstructions.Add(instruction);
        }

        if (!changed)
            return air;

        var result = new AbstractIR();
        result.AppendInstructions(context.NewInstructions);
        return result;
    }

    private static bool TryOptimizeExternalLoad(
        IReadOnlyList<Instruction> instructions,
        int index,
        CompilationContext context,
        IOptimizerIntrinsicCapabilityContext capabilityContext,
        out int consumedCount)
    {
        consumedCount = 0;

        if (index + 2 >= instructions.Count)
            return false;

        var loadEnvironment = instructions[index];
        var loadSlot = instructions[index + 1];
        var loadExternal = instructions[index + 2];

        if (!IsLoadEnvironmentCall(loadEnvironment))
            return false;

        if (loadSlot.UOpCode != UOpCode.Push || loadSlot.Operands.Count != 1 || loadSlot.Operands[0] is not int slot)
            return false;

        if (!TryGetLoadExternalValueType(loadExternal, out var valueType))
            return false;

        if (!capabilityContext.Supports(BuiltinIntrinsicSymbols.Core.LoadExternal, valueType))
            return false;

        context.NewInstructions.Add(BuiltinIntrinsicInstruction.Create(
            BuiltinIntrinsicSymbols.Core.LoadExternal,
            valueType,
            [slot]));
        consumedCount = 3;
        return true;
    }

    private static bool IsLoadEnvironmentCall(Instruction instruction)
    {
        return CSharpCallIntrinsicReader.TryGetCallDescriptor(instruction, out var descriptor)
               && descriptor.ReceiverKind == ManagedCallReceiverKind.ExecutionScopedProvider
               && descriptor.ExecutionScopedProviderType == typeof(ExternalRuntimeCallProvider)
               && descriptor.Method.Name == nameof(ExternalRuntimeCallProvider.LoadEnvironment);
    }

    private static bool TryGetLoadExternalValueType(Instruction instruction, out Type valueType)
    {
        valueType = default!;

        if (!CSharpCallIntrinsicReader.TryGetCallMethod(instruction, out var method))
            return false;

        if (!method.IsGenericMethod || method.GetGenericMethodDefinition() != typeof(ExternalRuntimeCalls)
                .GetMethod(nameof(ExternalRuntimeCalls.LoadExternal))
                .NotNull())
            return false;

        valueType = method.GetGenericArguments()[0];
        return true;
    }

    private class CompilationContext
    {
        public List<Instruction> NewInstructions { get; } =
        [
        ];
    }
}