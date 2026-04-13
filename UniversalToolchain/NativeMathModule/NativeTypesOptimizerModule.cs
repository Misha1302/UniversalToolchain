namespace NativeMathModule;

[DialectOptimizerAlias("NativeTypesOptimization")]
[DialectRuntimeExport("Optimizer", "NativeTypesOptimization")]
[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Native)]
public class NativeTypesOptimizerModule : IIRProcessingModule
{
    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
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
                IsNativeArithmeticPattern(instructions[i], instructions[i + 1], instructions[i + 2]))
            {
                var left = instructions[i].Operands[0];
                var right = instructions[i + 1].Operands[0];
                var method = (MethodInfo)instructions[i + 2].Operands[1];

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
                catch (TargetInvocationException)
                {
                    optimizedInstructions.Add(instructions[i]);
                }
                catch (ArgumentException)
                {
                    optimizedInstructions.Add(instructions[i]);
                }
                catch (TargetParameterCountException)
                {
                    optimizedInstructions.Add(instructions[i]);
                }
                catch (MethodAccessException)
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

    private bool IsNativeArithmeticPattern(Instruction inst1, Instruction inst2, Instruction inst3) =>
        inst1.UOpCode == UOpCode.Push &&
        inst2.UOpCode == UOpCode.Push &&
        inst3.UOpCode == UOpCode.Intrinsic &&
        inst3.Operands[0] is string name &&
        name == "call C#" &&
        inst3.Operands[1] is MethodInfo method &&
        method.DeclaringType == typeof(NativeArithmetic);
}
