namespace NativeMathModule;

[UniversalToolchain.Dialects.Abstractions.DialectOptimizerAlias("NativeCilOptimization")]
[AutoRegisterService]
public class NativeCilOptimizerModule : IIRProcessingModule
{
    // Словарь для маппинга типов на методы генерации CIL
    private static readonly Dictionary<Type, Action<Instruction, CompilationContext>> _cilGenerators = new();

    private static readonly IReadOnlyList<string> _standardModuleIntrinsics =
    [
        "load_i32",
        "load_i64",
        "load_f32",
        "load_f64"
    ];


    private static readonly IReadOnlyList<string> _decimalModuleIntrinsics =
    [
        "load_decimal"
    ];


    // Поддерживаемые нативные типы для оптимизации
    private static readonly HashSet<Type> _supportedTypes =
    [
        typeof(int),
        typeof(long),
        typeof(float),
        typeof(double),
        typeof(decimal)
    ];

    private bool _isDecimalsSupported;

    static NativeCilOptimizerModule()
    {
        InitializeCilGenerators();
    }

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        if (_standardModuleIntrinsics.Any(x => !compiler.SupportedIntrinsics.Contains(x)))
            return current;
        _isDecimalsSupported = _decimalModuleIntrinsics.All(x => compiler.SupportedIntrinsics.Contains(x));

        InitializeAirTypes();
        return OptimizeNativeLoads(current);
    }

    private void InitializeAirTypes()
    {
        AirTypes.TryRegisterIntrinsic(
            "load_i32",
            (_, stack) => stack.Push(typeof(int))
        );
        AirTypes.TryRegisterIntrinsic(
            "load_i64",
            (_, stack) => stack.Push(typeof(long))
        );
        AirTypes.TryRegisterIntrinsic(
            "load_f32",
            (_, stack) => stack.Push(typeof(float))
        );
        AirTypes.TryRegisterIntrinsic(
            "load_f64",
            (_, stack) => stack.Push(typeof(double))
        );

        if (_isDecimalsSupported)
            AirTypes.TryRegisterIntrinsic(
                "load_decimal",
                (_, stack) => stack.Push(typeof(decimal))
            );
    }

    private static void InitializeCilGenerators()
    {
        // Инициализация генераторов CIL для разных типов
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

            // Ищем паттерн: Push с примитивным типом
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
                    // Заменяем Push на наш CIL-интринсик
                    generator(instruction, context);
                    continue;
                }
            }

            // Для остальных инструкций оставляем как есть
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