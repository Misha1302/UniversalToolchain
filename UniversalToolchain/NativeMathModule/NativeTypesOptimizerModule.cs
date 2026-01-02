using System.Reflection;
using BasicCore;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;

namespace NativeMathModule;

[AutoRegisterService]
public class NativeTypesOptimizerModule : IIRProcessingModule
{
    // Кэш для скомпилированных операций
    private static readonly Dictionary<(Type, string), Delegate> _operationCache = new();

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        // Регистрируем обработку наших интринсиков
        InitIntrinsics();

        // Простая оптимизация: объединение последовательных операций
        return OptimizeNativeOperations(current);
    }

    public void InitMethodsTranslator(IAbstractMethodsTranslator methodsTranslator)
    {
        // Инициализация не требуется
    }

    private void InitIntrinsics()
    {
        // Используем существующую систему интринсиков
        // (если нужно будет добавить специальные интринсики для нативных типов)
    }

    private IAbstractIR OptimizeNativeOperations(IAbstractIR air)
    {
        var instructions = air.Instructions.ToList();
        var optimizedInstructions = new List<Instruction>();

        for (var i = 0; i < instructions.Count; i++)
        {
            // Ищем паттерн: две push-инструкции + вызов NativeArithmetic
            if (i + 2 < instructions.Count &&
                IsNativeArithmeticPattern(instructions[i], instructions[i + 1], instructions[i + 2]))
            {
                // Оптимизируем: заменяем на одну инструкцию с предварительно вычисленным значением
                var left = instructions[i].Operands[0];
                var right = instructions[i + 1].Operands[0];
                var method = (MethodInfo)instructions[i + 2].Operands[1];

                try
                {
                    var result = method.Invoke(null, new[] { left, right });
                    optimizedInstructions.Add(new Instruction(UOpCode.Push, [result]));
                    i += 2; // Пропускаем оптимизированные инструкции
                }
                catch
                {
                    // Если не удалось вычислить во время компиляции, оставляем как есть
                    optimizedInstructions.Add(instructions[i]);
                }
            }
            else
            {
                optimizedInstructions.Add(instructions[i]);
            }
        }

        var resultAir = new AbstractIR();
        resultAir.AppendInstructions(optimizedInstructions);
        return resultAir;
    }

    private bool IsNativeArithmeticPattern(Instruction inst1, Instruction inst2, Instruction inst3)
    {
        return inst1.UOpCode == UOpCode.Push &&
               inst2.UOpCode == UOpCode.Push &&
               inst3.UOpCode == UOpCode.Intrinsic &&
               inst3.Operands[0] is string name &&
               name == "call C#" &&
               inst3.Operands[1] is MethodInfo method &&
               method.DeclaringType == typeof(NativeArithmetic);
    }
}