namespace NativeMathModule;

[DialectOptimizerAlias("NativeTypesOptimization")]
[DialectRuntimeExport("Optimizer", "NativeTypesOptimization")]
[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Native)]
public class NativeTypesOptimizerModule : IAirOptimizer
{
    public IAbstractIR Optimize(IAbstractIR current)
    {
        InitIntrinsics();

        // Fold consecutive native arithmetic calls when operands are compile-time constants.
        return OptimizeNativeOperations(current);
    }

    public void InitMethodsTranslator(IAbstractMethodsTranslator methodsTranslator)
    {
    }

    private void InitIntrinsics()
    {
    }

    private IAbstractIR OptimizeNativeOperations(IAbstractIR air)
    {
        var instructions = air.Instructions.ToList();
        var optimizedInstructions = new List<Instruction>();

        for (var i = 0; i < instructions.Count; i++)
        {
            // Match: push, push, and then a NativeArithmetic call.
            if (i + 2 < instructions.Count &&
                TryGetNativeArithmeticMethod(instructions[i], instructions[i + 1], instructions[i + 2], out var method))
            {
                var left = instructions[i].Operands[0];
                var right = instructions[i + 1].Operands[0];

                try
                {
                    var result = method.Invoke(null, new[] { left, right });
                    if (result is not null)
                    {
                        optimizedInstructions.Add(new Instruction(UOpCode.Push, [result]));
                        i += 2;
                    }
                    else
                    {
                        optimizedInstructions.Add(instructions[i]);
                    }
                }
                catch
                {
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

    private static bool TryGetNativeArithmeticMethod(
        Instruction inst1,
        Instruction inst2,
        Instruction inst3,
        out MethodInfo method)
    {
        method = default!;
        return inst1.UOpCode == UOpCode.Push &&
               inst2.UOpCode == UOpCode.Push &&
               CSharpCallIntrinsicReader.TryGetCallMethod(inst3, out method) &&
               method.DeclaringType == typeof(NativeArithmetic);
    }
}