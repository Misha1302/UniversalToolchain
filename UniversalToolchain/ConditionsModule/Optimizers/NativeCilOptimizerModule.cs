using System.Reflection;
using BasicCore;
using DotnetAirHelper;
using IntermediateRepresentationAbstractions;
using ListExtensions;
using UniversalIntermediateRepresentation;

namespace ConditionsModule.Optimizers;

[AutoRegisterService]
public class BoolOptimizerModule : IIRProcessingModule
{
    private static readonly HashSet<string> _boolIntrinsics =
    [
        "load_bool",
        "bool_and",
        "bool_or",
        "bool_not",
        "bool_eq",
        "bool_neq",
        "bool_to_i32"
    ];

    private static bool _intrinsicsRegistered;

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        // Проверяем, поддерживает ли компилятор bool интринсики
        if (!_boolIntrinsics.All(x => compiler.SupportedIntrinsics.Contains(x)))
            return current;

        // Регистрируем интринсики в AirTypes
        if (!_intrinsicsRegistered)
        {
            RegisterBoolIntrinsics();
            _intrinsicsRegistered = true;
        }

        return OptimizeBoolOperations(current);
    }

    public void InitMethodsTranslator(IAbstractMethodsTranslator methodsTranslator)
    {
        // Не требуется для этого модуля
    }

    private void RegisterBoolIntrinsics()
    {
        AirTypes.TryRegisterIntrinsic("load_bool", (instruction, stack) =>
        {
            stack.Push(typeof(int)); // bool представляется как int32 в CIL
        });

        AirTypes.TryRegisterIntrinsic("bool_and", (instruction, stack) =>
        {
            stack.Pop(); // снимаем 2 значения
            stack.Pop();
            stack.Push(typeof(int)); // результат - int32
        });

        AirTypes.TryRegisterIntrinsic("bool_or", (instruction, stack) =>
        {
            stack.Pop();
            stack.Pop();
            stack.Push(typeof(int));
        });

        AirTypes.TryRegisterIntrinsic("bool_not", (instruction, stack) =>
        {
            stack.Pop();
            stack.Push(typeof(int));
        });

        AirTypes.TryRegisterIntrinsic("bool_eq", (instruction, stack) =>
        {
            stack.Pop();
            stack.Pop();
            stack.Push(typeof(int));
        });

        AirTypes.TryRegisterIntrinsic("bool_neq", (instruction, stack) =>
        {
            stack.Pop();
            stack.Pop();
            stack.Push(typeof(int));
        });

        AirTypes.TryRegisterIntrinsic("bool_to_i32", (instruction, stack) =>
        {
            // Ничего не меняем - уже int32
        });
    }

    private IAbstractIR OptimizeBoolOperations(IAbstractIR air)
    {
        var instructions = air.Instructions.ToList();
        var optimized = new List<Instruction>();

        foreach (var current in instructions)
        {
            if (!TryToOptimize(current, optimized))
                optimized.Add(current);
        }

        var result = new AbstractIR();
        result.AppendInstructions(optimized);
        return result;
    }

    private bool TryToOptimize(Instruction current, List<Instruction> optimized)
    {
        // 1. Оптимизация Push(true/false) -> load_bool
        if (current.UOpCode == UOpCode.Push && current.Operands.Count == 1 && current.Operands[0] is bool boolValue)
        {
            optimized.Add(new Instruction(
                UOpCode.Intrinsic,
                ["load_bool", boolValue])
            );
            return true;
        }

        // 2. Оптимизация вызовов BooleanOperations методов
        if (current.UOpCode == UOpCode.Intrinsic &&
            current.Operands.Count >= 2 &&
            current.Operands[0] is string intrinsicName &&
            intrinsicName == "call C#" &&
            current.Operands[1] is MethodInfo method)
        {
            var optimizedInstr = TryOptimizeBooleanMethodCall(method);
            if (optimizedInstr != null)
            {
                optimized.Add(optimizedInstr);
                return true;
            }
        }

        // 3. Оптимизация сравнений, возвращающих bool
        else if (current.UOpCode == UOpCode.Intrinsic &&
                 current.Operands.Count >= 2 &&
                 current.Operands[0] is string callName &&
                 callName == "call C#" &&
                 current.Operands[1] is MethodInfo comparisonMethod)
        {
            var optimizedComparison = TryOptimizeComparison(comparisonMethod);
            if (optimizedComparison != null)
                // Для сравнений обычно есть 2 аргумента на стеке
                if (optimized.Count >= 2)
                    // Проверяем, что последние 2 инструкции - Push (аргументы)
                    if (optimized[^1].UOpCode == UOpCode.Push &&
                        optimized[^2].UOpCode == UOpCode.Push)
                    {
                        // Заменяем оба Push и вызов метода на один интринсик
                        optimized.RemoveRange(optimized.Count - 2, 2);
                        optimized.Add(optimizedComparison);
                        return true;
                    }
        }

        return false;
    }

    private Instruction? TryOptimizeBooleanMethodCall(MethodInfo method)
    {
        if (method.DeclaringType?.Name == "BooleanOperations" ||
            method.DeclaringType?.IsGenericType == true &&
            method.DeclaringType.GetGenericTypeDefinition().Name == "BooleanOperations")
            return method.Name switch
            {
                "And" => new Instruction(UOpCode.Intrinsic, [
                    "bool_and"
                ]),
                "Or" => new Instruction(UOpCode.Intrinsic, [
                    "bool_or"
                ]),
                "Not" => new Instruction(UOpCode.Intrinsic, [
                    "bool_not"
                ]),
                _ => null
            };

        return null;
    }

    private Instruction? TryOptimizeComparison(MethodInfo method)
    {
        if (method.DeclaringType?.Name == "Comparisons")
            return method.Name switch
            {
                "Equal" => new Instruction(UOpCode.Intrinsic, [
                    "bool_eq"
                ]),
                "NotEqual" => new Instruction(UOpCode.Intrinsic, [
                    "bool_neq"
                ]),
                _ => null
            };

        return null;
    }
}