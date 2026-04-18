using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Capabilities;

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
            .Select(type => (BuiltinIntrinsicSymbols.Core.LoadConst, new[] { type }));
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

    private class CompilationContext
    {
        public List<Instruction> NewInstructions { get; } =
        [
        ];
    }
}