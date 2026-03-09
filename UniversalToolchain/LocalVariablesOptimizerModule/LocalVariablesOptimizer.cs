using System.Reflection;
using BasicCore.Attributes;
using BasicCore.Contracts;
using DotnetAirHelper;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using ListExtensions;
using ObjectExtensions;
using SettableGettableModule.Core;
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
        var instructions = optimized.Instructions.ToList();
        var changed = RemoveRedundantLocalRoundtrips(instructions);
        if (!changed)
            return optimized;

        var result = new AbstractIR();
        result.AppendInstructions(instructions);
        return result;
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
        var sourceIndexes = new List<int>();

        for (var i = 0; i < instructions.Count; i++)
        {
            // Store pattern: Push value, Push "varName", GetRef, SetValue
            if (i >= 2 && IsStorePattern(instructions[i - 2], instructions[i - 1], instructions[i]))
                // Remove only instructions that were really appended for this exact pattern.
                if (sourceIndexes.Count >= 2 &&
                    sourceIndexes[^2] == i - 2 &&
                    sourceIndexes[^1] == i - 1)
                {
                    newInstructions.RemoveRange(newInstructions.Count - 2, 2);
                    sourceIndexes.RemoveRange(sourceIndexes.Count - 2, 2);

                    // Create new store_local intrinsic
                    var storeLocalInstr = CreateStoreLocalIntrinsic(instructions[i - 2], instructions[i - 1]);
                    newInstructions.Add(storeLocalInstr);
                    sourceIndexes.Add(i);

                    // Skip the SetValue instruction
                    continue;
                }

            // Load pattern: Push "varName", Get
            if (i >= 1 && IsLoadPattern(instructions[i - 1], instructions[i]))
                // Remove only instruction that was really appended for this exact pattern.
                if (sourceIndexes.Count >= 1 && sourceIndexes[^1] == i - 1)
                {
                    newInstructions.RemoveAt(newInstructions.Count - 1);
                    sourceIndexes.RemoveAt(sourceIndexes.Count - 1);

                    // Create new load_local intrinsic
                    var loadLocalInstr = CreateLoadLocalIntrinsic(instructions[i - 1], instructions[i]);
                    newInstructions.Add(loadLocalInstr);
                    sourceIndexes.Add(i);

                    // Skip the Get instruction
                    continue;
                }

            newInstructions.Add(instructions[i]);
            sourceIndexes.Add(i);
        }

        var resultAir = new AbstractIR();
        resultAir.AppendInstructions(newInstructions);
        return resultAir;
    }

    private bool IsPushString(Instruction instr) =>
        instr.UOpCode == UOpCode.Push &&
        instr.Operands.Count == 1 &&
        instr.Operands[0] is string;

    private bool IsIntrinsicCallCSharp(Instruction instr, out MethodInfo? methodInfo)
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
        method.DeclaringType != null &&
        method.DeclaringType.IsGenericType &&
        method.DeclaringType.GetGenericTypeDefinition() == typeof(VariablesContainer<>);

    private bool IsSetValueMethod(MethodInfo method) =>
        method.Name == "SetValue" &&
        method.DeclaringType != null &&
        method.DeclaringType.IsGenericType &&
        method.DeclaringType.GetGenericTypeDefinition() == typeof(VariableReference<>);

    private bool IsSetValueToMethod(MethodInfo method) => method.Name == "SetValueTo" && method.IsGenericMethod;

    private bool IsGetMethod(MethodInfo method) =>
        method.Name == "Get" &&
        method.DeclaringType != null &&
        method.DeclaringType.IsGenericType &&
        method.DeclaringType.GetGenericTypeDefinition() == typeof(VariablesContainer<>);

    private bool IsStorePattern(Instruction pushInstr, Instruction getRefInstr, Instruction setValueInstr)
    {
        if (!IsPushString(pushInstr)) return false;
        if (!IsIntrinsicCallCSharp(getRefInstr, out var getRefMethod) || getRefMethod is null || !IsGetRefMethod(getRefMethod)) return false;
        if (!IsIntrinsicCallCSharp(setValueInstr, out var setValueMethod) || setValueMethod is null || !IsSetValueToMethod(setValueMethod)) return false;

        // Verify that the types match
        Thrower.AssertAlways(getRefMethod.DeclaringType != null);
        var varTypeFromGetRef = getRefMethod.DeclaringType.GetGenericArguments()[0];
        var varTypeFromSetValue = setValueMethod.GetGenericArguments()[0];
        return varTypeFromGetRef == varTypeFromSetValue;
    }

    private bool IsLoadPattern(Instruction pushInstr, Instruction getInstr)
    {
        if (!IsPushString(pushInstr)) return false;
        if (!IsIntrinsicCallCSharp(getInstr, out var getMethod) || getMethod is null || !IsGetMethod(getMethod)) return false;
        return true;
    }

    private bool IsLoadRefPattern(Instruction pushInstr, Instruction getRefInstr)
    {
        if (!IsPushString(pushInstr)) return false;
        if (!IsIntrinsicCallCSharp(getRefInstr, out var getRefMethod) || getRefMethod is null || !IsGetRefMethod(getRefMethod)) return false;
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

    private static bool RemoveRedundantLocalRoundtrips(List<Instruction> instructions)
    {
        var changed = false;

        var labelToIndex = new Dictionary<object, int>();
        for (var i = 0; i < instructions.Count; i++)
        {
            if (IsLabel(instructions[i], out var label))
                labelToIndex[label] = i;
        }

        var branchTargets = CollectBranchTargets(instructions, labelToIndex);
        var hasBackwardBranches = HasBackwardBranch(instructions, labelToIndex);

        var iIndex = 0;
        while (iIndex < instructions.Count - 1)
        {
            if (!hasBackwardBranches && CanApplyRule3(instructions, iIndex, branchTargets, out var replacement))
            {
                instructions[iIndex] = replacement;
                instructions.RemoveRange(iIndex + 1, 2);
                changed = true;
                continue;
            }

            if (!hasBackwardBranches && CanApplyRule2(instructions, iIndex, branchTargets))
            {
                instructions.RemoveRange(iIndex, 2);
                changed = true;
                continue;
            }

            if (CanApplyRule1(instructions, iIndex, branchTargets))
            {
                instructions.RemoveRange(iIndex, 2);
                changed = true;
                continue;
            }

            iIndex++;
        }

        return changed;
    }

    private static bool CanApplyRule1(IReadOnlyList<Instruction> instructions, int start, HashSet<int> branchTargets)
    {
        if (!CanRewriteSlice(instructions, start, 2, branchTargets))
            return false;

        return TryGetLoadLocalKey(instructions[start], out var loadKey) &&
               TryGetStoreLocalKey(instructions[start + 1], out var storeKey) &&
               loadKey == storeKey;
    }

    private static bool CanApplyRule2(IReadOnlyList<Instruction> instructions, int start, HashSet<int> branchTargets)
    {
        if (!CanRewriteSlice(instructions, start, 2, branchTargets))
            return false;

        if (!TryGetStoreLocalKey(instructions[start], out var localKey) ||
            !TryGetLoadLocalKey(instructions[start + 1], out var loadedKey) ||
            loadedKey != localKey)
            return false;

        return !HasLoadBeforeBoundary(instructions, start + 2, localKey);
    }

    private static bool CanApplyRule3(IReadOnlyList<Instruction> instructions, int start, HashSet<int> branchTargets, out Instruction replacement)
    {
        replacement = null!;
        if (start + 2 >= instructions.Count)
            return false;

        if (!CanRewriteSlice(instructions, start, 3, branchTargets))
            return false;

        if (!TryGetStoreLocalKey(instructions[start], out var localKey) ||
            !TryGetLoadLocalKey(instructions[start + 1], out var loadedKey) ||
            loadedKey != localKey ||
            !TryGetStoreLocalKey(instructions[start + 2], out _))
            return false;

        if (HasLoadBeforeBoundary(instructions, start + 3, localKey))
            return false;

        replacement = instructions[start + 2];
        return true;
    }

    private static bool CanRewriteSlice(IReadOnlyList<Instruction> instructions, int start, int length, HashSet<int> branchTargets)
    {
        if (start + length > instructions.Count)
            return false;

        for (var i = start; i < start + length; i++)
        {
            if (branchTargets.Contains(i))
                return false;
            if (i > 0 && instructions[i - 1].UOpCode == UOpCode.Label)
                return false;
        }

        return true;
    }

    private static bool HasLoadBeforeBoundary(IReadOnlyList<Instruction> instructions, int start, string localKey)
    {
        for (var i = start; i < instructions.Count; i++)
        {
            if (IsBlockBoundary(instructions[i]))
                return false;
            if (TryGetLoadLocalKey(instructions[i], out var key) && key == localKey)
                return true;
        }

        return false;
    }

    private static bool IsBlockBoundary(Instruction instruction) =>
        instruction.UOpCode == UOpCode.Label ||
        instruction.UOpCode == UOpCode.Jmp ||
        instruction.UOpCode == UOpCode.JmpIf ||
        instruction.UOpCode == UOpCode.JmpIfNot ||
        IsIntrinsicName(instruction, "ret") ||
        IsIntrinsicName(instruction, "throw") ||
        IsIntrinsicBranch(instruction);

    private static bool IsIntrinsicBranch(Instruction instruction)
    {
        if (instruction.UOpCode != UOpCode.Intrinsic || instruction.Operands.Count == 0 || instruction.Operands[0] is not string name)
            return false;

        return name == "switch" ||
               name == "leave" ||
               name.StartsWith("br", StringComparison.Ordinal);
    }

    private static bool IsIntrinsicName(Instruction instruction, string name) =>
        instruction.UOpCode == UOpCode.Intrinsic &&
        instruction.Operands.Count > 0 &&
        instruction.Operands[0] is string intrinsicName &&
        intrinsicName == name;

    private static HashSet<int> CollectBranchTargets(IReadOnlyList<Instruction> instructions, IReadOnlyDictionary<object, int> labelToIndex)
    {
        var result = new HashSet<int>();
        for (var i = 0; i < instructions.Count; i++)
        {
            foreach (var target in ExtractBranchTargets(instructions[i]))
            {
                if (labelToIndex.TryGetValue(target, out var index))
                    result.Add(index);
            }
        }

        return result;
    }

    private static bool HasBackwardBranch(IReadOnlyList<Instruction> instructions, IReadOnlyDictionary<object, int> labelToIndex)
    {
        for (var i = 0; i < instructions.Count; i++)
        {
            foreach (var target in ExtractBranchTargets(instructions[i]))
            {
                if (labelToIndex.TryGetValue(target, out var targetIndex) && targetIndex <= i)
                    return true;
            }
        }

        return false;
    }

    private static IEnumerable<object> ExtractBranchTargets(Instruction instruction)
    {
        if ((instruction.UOpCode == UOpCode.Jmp || instruction.UOpCode == UOpCode.JmpIf || instruction.UOpCode == UOpCode.JmpIfNot) &&
            instruction.Operands.Count > 0)
            yield return instruction.Operands[0];

        if (instruction.UOpCode == UOpCode.Intrinsic && instruction.Operands.Count > 1 && instruction.Operands[0] is string name)
        {
            if (name == "switch" && instruction.Operands[1] is IEnumerable<object> labels)
                foreach (var label in labels)
                    yield return label;
            else if ((name == "leave" || name.StartsWith("br", StringComparison.Ordinal)) && instruction.Operands[1] is not null)
                yield return instruction.Operands[1];
        }
    }

    private static bool IsLabel(Instruction instruction, out object label)
    {
        if (instruction.UOpCode == UOpCode.Label && instruction.Operands.Count > 0)
        {
            label = instruction.Operands[0];
            return true;
        }

        label = null!;
        return false;
    }

    private static bool TryGetLoadLocalKey(Instruction instruction, out string localKey)
    {
        localKey = string.Empty;
        if (instruction.UOpCode != UOpCode.Intrinsic || instruction.Operands.Count == 0 || instruction.Operands[0] is not string name)
            return false;

        if (name == "load_local" && instruction.Operands.Count > 1)
        {
            localKey = instruction.Operands[1].ToString().NotNull();
            return true;
        }

        return TryGetNormalizedLocalFromCilName(name, instruction.Operands, "ldloc", out localKey);
    }

    private static bool TryGetStoreLocalKey(Instruction instruction, out string localKey)
    {
        localKey = string.Empty;
        if (instruction.UOpCode != UOpCode.Intrinsic || instruction.Operands.Count == 0 || instruction.Operands[0] is not string name)
            return false;

        if (name == "store_local" && instruction.Operands.Count > 1)
        {
            localKey = instruction.Operands[1].ToString().NotNull();
            return true;
        }

        return TryGetNormalizedLocalFromCilName(name, instruction.Operands, "stloc", out localKey);
    }

    private static bool TryGetNormalizedLocalFromCilName(string name, IReadOnlyList<object> operands, string prefix, out string localKey)
    {
        localKey = string.Empty;
        if (name == prefix || name == $"{prefix}.s")
        {
            if (operands.Count < 2)
                return false;

            localKey = operands[1].ToString().NotNull();
            return true;
        }

        if (name.StartsWith($"{prefix}.", StringComparison.Ordinal) &&
            int.TryParse(name[(prefix.Length + 1)..], out var shortIndex))
        {
            localKey = shortIndex.ToString();
            return true;
        }

        return false;
    }
}