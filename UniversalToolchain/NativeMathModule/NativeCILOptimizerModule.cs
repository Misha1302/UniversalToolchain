using BasicCore;
using DotnetAirHelper;
using IntermediateRepresentationAbstractions;
using ListExtensions;
using ObjectExtensions;
using UniversalIntermediateRepresentation;

namespace NativeMathModule;

[AutoRegisterService]
public class NativeCilOptimizerModule : IIRProcessingModule
{
    // Словарь для маппинга типов на методы генерации CIL
    private static readonly Dictionary<Type, Action<Instruction, CompilationContext>> _cilGenerators = new();

    // Поддерживаемые нативные типы для оптимизации
    private static readonly HashSet<Type> _supportedTypes =
    [
        typeof(int),
        typeof(uint),

        typeof(long),
        typeof(ulong),

        typeof(float),
        typeof(double),

        typeof(bool),
        typeof(byte),
        typeof(sbyte),

        typeof(short),
        typeof(ushort),

        typeof(char)
    ];

    static NativeCilOptimizerModule()
    {
        InitializeCilGenerators();
    }

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        // Проверяем, поддерживает ли компилятор наши интринсики
        if (!compiler.SupportedIntrinsics.Contains("load_i32") &&
            !compiler.SupportedIntrinsics.Contains("load_i64") &&
            !compiler.SupportedIntrinsics.Contains("load_f32") &&
            !compiler.SupportedIntrinsics.Contains("load_f64"))
            // Если компилятор не поддерживает наши интринсики, возвращаем как есть
            return current;

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

        _cilGenerators[typeof(bool)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<bool>();
            context.NewInstructions.Add(new Instruction(
                UOpCode.Intrinsic,
                ["load_i32", value ? 1 : 0]
            ));
        };

        _cilGenerators[typeof(byte)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<byte>();
            context.NewInstructions.Add(new Instruction(
                UOpCode.Intrinsic,
                ["load_i32", (int)value]
            ));
        };

        _cilGenerators[typeof(sbyte)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<sbyte>();
            context.NewInstructions.Add(new Instruction(
                UOpCode.Intrinsic,
                ["load_i32", (int)value]
            ));
        };

        _cilGenerators[typeof(short)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<short>();
            context.NewInstructions.Add(new Instruction(
                UOpCode.Intrinsic,
                ["load_i32", (int)value]
            ));
        };

        _cilGenerators[typeof(ushort)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<ushort>();
            context.NewInstructions.Add(new Instruction(
                UOpCode.Intrinsic,
                ["load_i32", (int)value]
            ));
        };

        _cilGenerators[typeof(char)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<char>();
            context.NewInstructions.Add(new Instruction(
                UOpCode.Intrinsic,
                ["load_i32", (int)value]
            ));
        };

        _cilGenerators[typeof(uint)] = (instruction, context) =>
        {
            var value = instruction.Operands[0].Get<uint>();
            context.NewInstructions.Add(new Instruction(
                UOpCode.Intrinsic,
                ["load_i32", (int)value]
            ));
        };

        _cilGenerators[typeof(ulong)] = (instruction, context) =>
        {
            // Для ulong преобразуем в два int32
            var value = instruction.Operands[0].Get<ulong>();
            context.NewInstructions.Add(new Instruction(
                UOpCode.Intrinsic,
                ["load_i64", (long)value]
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