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
    // Maps constants to legacy CIL load intrinsics for backends that support them.
    private static readonly Dictionary<Type, Action<Instruction, CompilationContext>> _cilGenerators = new();

    private static readonly IReadOnlyList<Type> _standardLoadTypes =
    [
        typeof(int),
        typeof(long),
        typeof(float),
        typeof(double)
    ];

    // Supported native types for load-constant lowering.
    private static readonly HashSet<Type> _supportedTypes =
    [
        typeof(int),
        typeof(long),
        typeof(float),
        typeof(double),
        typeof(decimal)
    ];

    private IOptimizerIntrinsicCapabilityContext? _capabilityContext;
    private bool _isDecimalsSupported;

    static NativeCilOptimizerModule()
    {
        InitializeCilGenerators();
    }

    public void InitIntrinsicCapabilityContext(IOptimizerIntrinsicCapabilityContext capabilityContext)
    {
        _capabilityContext = capabilityContext ?? throw new ArgumentNullException(nameof(capabilityContext));
    }

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        var capabilityContext = _capabilityContext
                                ?? throw new InvalidOperationException("Native CIL optimizer requires intrinsic capability context initialization.");

        if (!_standardLoadTypes.All(type => capabilityContext.Supports(BuiltinIntrinsicSymbols.Core.LoadConst, type)))
            return current;

        _isDecimalsSupported = capabilityContext.Supports(BuiltinIntrinsicSymbols.Core.LoadConst, typeof(decimal));

        return OptimizeNativeLoads(current);
    }

    private static void InitializeCilGenerators()
    {
        _cilGenerators[typeof(int)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<int>();
            context.NewInstructions.Add(new Instruction(
                UOpCode.Intrinsic,
                ["load_i32", value]
            ));
        };

        _cilGenerators[typeof(long)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<long>();
            context.NewInstructions.Add(new Instruction(
                UOpCode.Intrinsic,
                ["load_i64", value]
            ));
        };

        _cilGenerators[typeof(float)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<float>();
            context.NewInstructions.Add(new Instruction(
                UOpCode.Intrinsic,
                ["load_f32", value]
            ));
        };

        _cilGenerators[typeof(double)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<double>();
            context.NewInstructions.Add(new Instruction(
                UOpCode.Intrinsic,
                ["load_f64", value]
            ));
        };

        _cilGenerators[typeof(decimal)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<decimal>();
            context.NewInstructions.Add(new Instruction(
                UOpCode.Intrinsic,
                ["load_decimal", value]
            ));
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

                if (valueType == typeof(decimal) && !_isDecimalsSupported)
                {
                    context.NewInstructions.Add(instruction);
                    continue;
                }

                if (_supportedTypes.Contains(valueType) && _cilGenerators.TryGetValue(valueType, out var generator))
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
