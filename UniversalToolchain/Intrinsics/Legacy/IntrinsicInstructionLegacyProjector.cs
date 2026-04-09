using IntermediateRepresentationAbstractions;
using ObjectExtensions;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Capabilities;
using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Legacy;

internal static class IntrinsicInstructionLegacyProjector
{
    public static bool TryProject(Instruction instruction, out Instruction projectedInstruction)
    {
        if (instruction == null)
            Thrower.ArgumentNull(nameof(instruction));

        projectedInstruction = default!;

        if (instruction.UOpCode != UOpCode.Intrinsic)
            return false;

        if (instruction.Operands.Count == 0)
            return false;

        if (instruction.Operands[0] is string)
        {
            projectedInstruction = instruction;
            return true;
        }

        if (instruction.Operands.Count != 1 || instruction.Operands[0] is not IntrinsicInvocation invocation)
            return false;

        if (!LegacyCapabilityNameEncoder.TryEncode(invocation.Symbol, invocation.TypeArguments, out var intrinsicName))
            return false;

        if (invocation.Symbol == BuiltinIntrinsicSymbols.Core.CallCSharp
            || invocation.Symbol == BuiltinIntrinsicSymbols.Core.CallCSharpCtor)
        {
            if (!TryGetDataOperand(invocation.DataOperands, 0, out var dataOperand))
                return false;

            projectedInstruction = new Instruction(UOpCode.Intrinsic, [intrinsicName, dataOperand]);
            return true;
        }

        if (invocation.Symbol == BuiltinIntrinsicSymbols.Core.LoadExternal)
        {
            if (!TryGetDataOperand(invocation.DataOperands, 0, out var dataOperand)
                || !TryGetSingleRuntimeType(invocation.TypeArguments, out var runtimeType))
                return false;

            projectedInstruction = new Instruction(UOpCode.Intrinsic, [intrinsicName, dataOperand, runtimeType]);
            return true;
        }

        if (invocation.Symbol == BuiltinIntrinsicSymbols.Core.StoreExternal
            || invocation.Symbol == BuiltinIntrinsicSymbols.Core.LoadConst)
        {
            if (!TryGetDataOperand(invocation.DataOperands, 0, out var dataOperand))
                return false;

            projectedInstruction = new Instruction(UOpCode.Intrinsic, [intrinsicName, dataOperand]);
            return true;
        }

        if (invocation.Symbol == BuiltinIntrinsicSymbols.Storage.LoadLocal
            || invocation.Symbol == BuiltinIntrinsicSymbols.Storage.LoadLocalRef)
        {
            if (!TryGetDataOperand(invocation.DataOperands, 0, out var dataOperand)
                || !TryGetSingleRuntimeType(invocation.TypeArguments, out var runtimeType))
                return false;

            projectedInstruction = new Instruction(UOpCode.Intrinsic, [intrinsicName, dataOperand, runtimeType]);
            return true;
        }

        if (invocation.Symbol == BuiltinIntrinsicSymbols.Storage.StoreLocal)
        {
            if (!TryGetDataOperand(invocation.DataOperands, 0, out var dataOperand))
                return false;

            if (TryGetSingleRuntimeType(invocation.TypeArguments, out var runtimeTypeFromTypeArguments))
            {
                projectedInstruction = new Instruction(UOpCode.Intrinsic, [intrinsicName, dataOperand, runtimeTypeFromTypeArguments]);
                return true;
            }

            if (invocation.TypeArguments.Count == 0
                && invocation.DataOperands.Count >= 2
                && invocation.DataOperands[1] is Type runtimeTypeFromData)
            {
                projectedInstruction = new Instruction(UOpCode.Intrinsic, [intrinsicName, dataOperand, runtimeTypeFromData]);
                return true;
            }

            return false;
        }

        if (invocation.Symbol == BuiltinIntrinsicSymbols.Boolean.And
            || invocation.Symbol == BuiltinIntrinsicSymbols.Boolean.Or
            || invocation.Symbol == BuiltinIntrinsicSymbols.Boolean.Not)
        {
            projectedInstruction = new Instruction(UOpCode.Intrinsic, [intrinsicName]);
            return true;
        }

        if (invocation.Symbol.Namespace == BuiltinIntrinsicSymbols.Arithmetic.Add.Namespace
            || invocation.Symbol.Namespace == BuiltinIntrinsicSymbols.Comparison.Equal.Namespace)
        {
            projectedInstruction = new Instruction(UOpCode.Intrinsic, [intrinsicName]);
            return true;
        }

        return false;
    }


    private static bool TryGetDataOperand(IReadOnlyList<object?> dataOperands, int index, out object operand)
    {
        operand = default!;

        if (dataOperands.Count <= index || dataOperands[index] == null)
            return false;

        operand = dataOperands[index]!;
        return true;
    }

    private static bool TryGetSingleRuntimeType(IReadOnlyList<IntrinsicTypeArgument> typeArguments, out Type runtimeType)
    {
        runtimeType = default!;

        if (typeArguments.Count != 1)
            return false;

        runtimeType = typeArguments[0].RuntimeType;
        return true;
    }
}
