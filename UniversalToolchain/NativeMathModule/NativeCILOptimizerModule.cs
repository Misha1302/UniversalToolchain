using System.Reflection;
using BasicCore.Builtins;
using BasicCore.Capabilities;
using BasicCore.Core;
using BasicCore.Execution;
using UniversalToolchain.Dialects.Integration;

namespace NativeMathModule;

[DialectOptimizerAlias("NativeCilOptimization")]
[DialectRuntimeExport("Optimizer", "NativeCilOptimization")]
[AutoRegisterService]
[IntrinsicDescriptorProvider(typeof(CoreIntrinsicDescriptorProvider))]
[ArithmeticModeCompatibility(ArithmeticMode.Native)]
public class NativeCilOptimizerModule : IIRProcessingModule
{
    // Maps constants to typed load-constant intrinsics for backends that support them.
    private static readonly Dictionary<Type, Action<Instruction, CompilationContext>> _cilGenerators = new();

    private static readonly IReadOnlyList<Type> _supportedLoadTypes =
    [
        typeof(int),
        typeof(long),
        typeof(float),
        typeof(double),
        typeof(decimal)
    ];

    private IOptimizerIntrinsicCapabilityContext? _capabilityContext;

    static NativeCilOptimizerModule()
    {
        InitializeCilGenerators();
    }

    public void InitIntrinsicCapabilityContext(IOptimizerIntrinsicCapabilityContext capabilityContext)
    {
        _capabilityContext = capabilityContext.NotNull("Argument 'capabilityContext' cannot be null.");
    }

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        var capabilityContext = _capabilityContext.NotNull(
            "Native CIL optimizer requires intrinsic capability context initialization.");

        var requirements = _supportedLoadTypes
            .Select(type => (BuiltinIntrinsicSymbols.Core.LoadConst, new[] { type }))
            .Append((BuiltinIntrinsicSymbols.Core.LoadExternal, new[] { typeof(double) }));
        if (!OptimizerCapabilityGuards.SupportsAll(capabilityContext, requirements))
            return current;

        return OptimizeNativeLoads(current);
    }

    private static void InitializeCilGenerators()
    {
        _cilGenerators[typeof(int)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<int>();
            context.NewInstructions.Add(BuiltinIntrinsicInstruction.Create(
                BuiltinIntrinsicSymbols.Core.LoadConst,
                typeof(int),
                [value]));
        };

        _cilGenerators[typeof(long)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<long>();
            context.NewInstructions.Add(BuiltinIntrinsicInstruction.Create(
                BuiltinIntrinsicSymbols.Core.LoadConst,
                typeof(long),
                [value]));
        };

        _cilGenerators[typeof(float)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<float>();
            context.NewInstructions.Add(BuiltinIntrinsicInstruction.Create(
                BuiltinIntrinsicSymbols.Core.LoadConst,
                typeof(float),
                [value]));
        };

        _cilGenerators[typeof(double)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<double>();
            context.NewInstructions.Add(BuiltinIntrinsicInstruction.Create(
                BuiltinIntrinsicSymbols.Core.LoadConst,
                typeof(double),
                [value]));
        };

        _cilGenerators[typeof(decimal)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<decimal>();
            context.NewInstructions.Add(BuiltinIntrinsicInstruction.Create(
                BuiltinIntrinsicSymbols.Core.LoadConst,
                typeof(decimal),
                [value]));
        };
    }

    private IAbstractIR OptimizeNativeLoads(IAbstractIR air)
    {
        var instructions = air.Instructions.ToList();
        var context = new CompilationContext();

        for (var i = 0; i < instructions.Count; i++)
        {
            if (TryOptimizeExternalLoad(instructions, i, context, out var consumedCount))
            {
                i += consumedCount - 1;
                continue;
            }

            var instruction = instructions[i];

            if (instruction.UOpCode == UOpCode.Push && instruction.Operands.Count == 1)
            {
                var value = instruction.Operands[0];
                var valueType = value.GetType();

                if (_cilGenerators.TryGetValue(valueType, out var generator))
                {
                    generator(instruction, context);
                    continue;
                }
            }

            context.NewInstructions.Add(instruction);
        }

        var result = new AbstractIR();
        result.AppendInstructions(context.NewInstructions);
        return result;
    }

    private static bool TryOptimizeExternalLoad(
        IReadOnlyList<Instruction> instructions,
        int index,
        CompilationContext context,
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

        context.NewInstructions.Add(BuiltinIntrinsicInstruction.Create(
            BuiltinIntrinsicSymbols.Core.LoadExternal,
            valueType,
            [slot]));
        consumedCount = 3;
        return true;
    }

    private static bool IsLoadEnvironmentCall(Instruction instruction)
    {
        if (!IntrinsicInstructionNormalizer.TryNormalize(instruction, out var normalized))
            return false;

        if (normalized.Operands.Count < 2 || normalized.Operands[0].Get<string>() != "call C#")
            return false;

        return normalized.Operands[1] is CSharpCallDescriptor descriptor
               && descriptor.Receiver is CSharpCallReceiver.ExecutionScopedProvider provider
               && provider.ProviderType == typeof(ExternalRuntimeCallProvider)
               && descriptor.Method.Name == nameof(ExternalRuntimeCallProvider.LoadEnvironment);
    }

    private static bool TryGetLoadExternalValueType(Instruction instruction, out Type valueType)
    {
        valueType = default!;

        if (!IntrinsicInstructionNormalizer.TryNormalize(instruction, out var normalized))
            return false;

        if (normalized.Operands.Count < 2 || normalized.Operands[0].Get<string>() != "call C#")
            return false;

        var operand = normalized.Operands[1];
        var method = operand as MethodInfo;
        if (operand is CSharpCallDescriptor descriptor)
            method = descriptor.Method;

        if (method == null)
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