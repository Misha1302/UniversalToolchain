using System.Reflection;
using BasicCore;
using DotnetAirHelper;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using ListExtensions;
using ObjectExtensions;
using SettableGettableModule;
using UniversalIntermediateRepresentation;

namespace LocalVariablesOptimizerModule;

[AutoRegisterService]
public class LocalVariablesOptimizer : IIRProcessingModule
{
    private readonly IReadOnlyList<string> _intrinsics =
    [
        "store_local",
        "load_local",
        "load_local_ref"
    ];

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        if (!_intrinsics.All(x => compiler.SupportedIntrinsics.Contains(x)))
            return current;

        InitIntrinsics();
        var optimized = OptimizeVariables(current);
        return optimized;
    }

    private void InitIntrinsics()
    {
        AirTypes.TryRegisterIntrinsic(
            "store_local",
            (_, stack) => stack.Pop()
        );
        AirTypes.TryRegisterIntrinsic(
            "load_local",
            (instruction, stack) => stack.Push(instruction.Operands[2].Get<Type>())
        );
        AirTypes.TryRegisterIntrinsic(
            "load_local_ref",
            (instruction, stack) => stack.Push(typeof(VariableReference<>).MakeGenericType(instruction.Operands[2].Get<Type>()))
        );
    }

    private IAbstractIR OptimizeVariables(IAbstractIR air)
    {
        var instructions = air.Instructions.ToList();
        var newInstructions = new List<Instruction>();

        for (var i = 0; i < instructions.Count; i++)
        {
            // Store pattern: Push value, Push "varName", GetRef, SetValue
            if (i >= 2 && IsStorePattern(instructions[i - 2], instructions[i - 1], instructions[i]))
            {
                // Remove the last two instructions from newInstructions (push string and GetRef)
                if (newInstructions.Count >= 2)
                {
                    newInstructions.RemoveRange(newInstructions.Count - 2, 2);
                }

                // Create new store_local intrinsic
                var storeLocalInstr = CreateStoreLocalIntrinsic(instructions[i - 2], instructions[i - 1]);
                newInstructions.Add(storeLocalInstr);

                // Skip the SetValue instruction
                continue;
            }

            // Load pattern: Push "varName", Get
            if (i >= 1 && IsLoadPattern(instructions[i - 1], instructions[i]))
            {
                // Remove the last instruction from newInstructions (push string)
                if (newInstructions.Count >= 1)
                {
                    newInstructions.RemoveAt(newInstructions.Count - 1);
                }

                // Create new load_local intrinsic
                var loadLocalInstr = CreateLoadLocalIntrinsic(instructions[i - 1], instructions[i]);
                newInstructions.Add(loadLocalInstr);

                // Skip the Get instruction
                continue;
            }

            newInstructions.Add(instructions[i]);
        }

        var resultAir = new AbstractIR();
        resultAir.AppendInstructions(newInstructions);
        return resultAir;
    }

    private bool IsPushString(Instruction instr) =>
        instr.UOpCode == UOpCode.Push &&
        instr.Operands.Count == 1 &&
        instr.Operands[0] is string;

    private bool IsIntrinsicCallCSharp(Instruction instr, out MethodInfo methodInfo)
    {
        if (instr.UOpCode == UOpCode.Intrinsic &&
            instr.Operands.Count >= 2 &&
            instr.Operands[0] is string name &&
            name == "call C#")
        {
            methodInfo = instr.Operands[1] as MethodInfo;
            return methodInfo != null;
        }
        methodInfo = null;
        return false;
    }

    private bool IsGetRefMethod(MethodInfo method) =>
        method.Name == "GetRef" &&
        method.DeclaringType.IsGenericType &&
        method.DeclaringType.GetGenericTypeDefinition() == typeof(VariablesContainer<>);

    private bool IsSetValueMethod(MethodInfo method) =>
        method.Name == "SetValue" &&
        method.DeclaringType.IsGenericType &&
        method.DeclaringType.GetGenericTypeDefinition() == typeof(VariableReference<>);

    private bool IsSetValueToMethod(MethodInfo method) => method.Name == "SetValueTo" && method.IsGenericMethod;

    private bool IsGetMethod(MethodInfo method) =>
        method.Name == "Get" &&
        method.DeclaringType.IsGenericType &&
        method.DeclaringType.GetGenericTypeDefinition() == typeof(VariablesContainer<>);

    private bool IsStorePattern(Instruction pushInstr, Instruction getRefInstr, Instruction setValueInstr)
    {
        if (!IsPushString(pushInstr)) return false;
        if (!IsIntrinsicCallCSharp(getRefInstr, out var getRefMethod) || !IsGetRefMethod(getRefMethod)) return false;
        if (!IsIntrinsicCallCSharp(setValueInstr, out var setValueMethod) || !IsSetValueToMethod(setValueMethod)) return false;

        // Verify that the types match
        Thrower.AssertAlways(getRefMethod.DeclaringType != null);
        var varTypeFromGetRef = getRefMethod.DeclaringType.GetGenericArguments()[0];
        var varTypeFromSetValue = setValueMethod.GetGenericArguments()[0];
        return varTypeFromGetRef == varTypeFromSetValue;
    }

    private bool IsLoadPattern(Instruction pushInstr, Instruction getInstr)
    {
        if (!IsPushString(pushInstr)) return false;
        if (!IsIntrinsicCallCSharp(getInstr, out var getMethod) || !IsGetMethod(getMethod)) return false;
        return true;
    }

    private bool IsLoadRefPattern(Instruction pushInstr, Instruction getRefInstr)
    {
        if (!IsPushString(pushInstr)) return false;
        if (!IsIntrinsicCallCSharp(getRefInstr, out var getRefMethod) || !IsGetRefMethod(getRefMethod)) return false;
        return true;
    }

    private Instruction CreateStoreLocalIntrinsic(Instruction pushInstr, Instruction getRefInstr)
    {
        var varName = (string)pushInstr.Operands[0];
        var varType = getRefInstr.Operands[1].Get<MethodInfo>().DeclaringType.NotNull().GetGenericArguments()[0];
        return new Instruction(UOpCode.Intrinsic, ["store_local", varName, varType]);
    }

    private Instruction CreateLoadLocalIntrinsic(Instruction pushInstr, Instruction getInstr)
    {
        var varName = (string)pushInstr.Operands[0];
        var varType = getInstr.Operands[1].Get<MethodInfo>().DeclaringType.NotNull().GetGenericArguments()[0];
        return new Instruction(UOpCode.Intrinsic, ["load_local", varName, varType]);
    }

    private Instruction CreateLoadLocalRefIntrinsic(Instruction pushInstr, Instruction getRefInstr)
    {
        var varName = (string)pushInstr.Operands[0];
        var varType = getRefInstr.Operands[1].Get<MethodInfo>().DeclaringType.NotNull().GetGenericArguments()[0];
        return new Instruction(UOpCode.Intrinsic, ["load_local_ref", varName, varType]);
    }
}