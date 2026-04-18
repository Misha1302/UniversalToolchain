using BasicCore.Builtins;
using BasicCore.Capabilities;

namespace BasicCore.Core;

internal static class IntrinsicInstructionNormalizer
{
    public static Instruction NormalizeOrThrow(Instruction instruction)
    {
        if (!TryNormalize(instruction, out var normalizedInstruction))
            return Thrower.InvalidOpEx<Instruction>($"Unsupported intrinsic instruction payload: {instruction}");

        return normalizedInstruction;
    }

    public static bool TryNormalize(Instruction instruction, out Instruction normalizedInstruction)
    {
        instruction = instruction.ArgNotNull();

        normalizedInstruction = default!;

        if (instruction.UOpCode != UOpCode.Intrinsic)
            return false;

        if (instruction.Operands.Count == 0)
            return false;

        if (instruction.Operands[0] is string intrinsicName)
        {
            if (!IsSupportedIntrinsicName(intrinsicName))
                return false;

            normalizedInstruction = instruction;
            return true;
        }

        if (instruction.Operands.Count != 1 || instruction.Operands[0] is not IntrinsicInvocation invocation)
            return false;

        if (!LegacyCapabilityNameEncoder.TryEncode(invocation.Symbol, invocation.TypeArguments, out var encodedName))
            return false;

        if (invocation.Symbol == BuiltinIntrinsicSymbols.Core.CallCSharp
            || invocation.Symbol == BuiltinIntrinsicSymbols.Core.CallCSharpCtor)
        {
            if (!TryGetDataOperand(invocation.DataOperands, 0, out var dataOperand))
                return false;

            normalizedInstruction = new Instruction(UOpCode.Intrinsic, [encodedName, dataOperand]);
            return true;
        }

        if (invocation.Symbol == BuiltinIntrinsicSymbols.Core.LoadExternal)
        {
            if (!TryGetDataOperand(invocation.DataOperands, 0, out var dataOperand)
                || !TryGetSingleRuntimeType(invocation.TypeArguments, out var runtimeType))
                return false;

            normalizedInstruction = new Instruction(UOpCode.Intrinsic, [encodedName, dataOperand, runtimeType]);
            return true;
        }

        if (invocation.Symbol == BuiltinIntrinsicSymbols.Core.StoreExternal
            || invocation.Symbol == BuiltinIntrinsicSymbols.Core.LoadConst)
        {
            if (!TryGetDataOperand(invocation.DataOperands, 0, out var dataOperand))
                return false;

            normalizedInstruction = new Instruction(UOpCode.Intrinsic, [encodedName, dataOperand]);
            return true;
        }

        if (invocation.Symbol == BuiltinIntrinsicSymbols.Storage.LoadLocal
            || invocation.Symbol == BuiltinIntrinsicSymbols.Storage.LoadLocalRef)
        {
            if (!TryGetDataOperand(invocation.DataOperands, 0, out var dataOperand)
                || !TryGetSingleRuntimeType(invocation.TypeArguments, out var runtimeType))
                return false;

            normalizedInstruction = new Instruction(UOpCode.Intrinsic, [encodedName, dataOperand, runtimeType]);
            return true;
        }

        if (invocation.Symbol == BuiltinIntrinsicSymbols.Storage.StoreLocal)
        {
            if (!TryGetDataOperand(invocation.DataOperands, 0, out var dataOperand))
                return false;

            if (TryGetSingleRuntimeType(invocation.TypeArguments, out var runtimeTypeFromTypeArguments))
            {
                normalizedInstruction = new Instruction(UOpCode.Intrinsic, [encodedName, dataOperand, runtimeTypeFromTypeArguments]);
                return true;
            }

            if (invocation.TypeArguments.Count == 0
                && invocation.DataOperands.Count >= 2
                && invocation.DataOperands[1] is Type runtimeTypeFromData)
            {
                normalizedInstruction = new Instruction(UOpCode.Intrinsic, [encodedName, dataOperand, runtimeTypeFromData]);
                return true;
            }

            return false;
        }

        if (invocation.Symbol == BuiltinIntrinsicSymbols.Boolean.And
            || invocation.Symbol == BuiltinIntrinsicSymbols.Boolean.Or
            || invocation.Symbol == BuiltinIntrinsicSymbols.Boolean.Not)
        {
            normalizedInstruction = new Instruction(UOpCode.Intrinsic, [encodedName]);
            return true;
        }

        if (invocation.Symbol.Namespace == BuiltinIntrinsicSymbols.Arithmetic.Add.Namespace
            || invocation.Symbol.Namespace == BuiltinIntrinsicSymbols.Comparison.Equal.Namespace)
        {
            normalizedInstruction = new Instruction(UOpCode.Intrinsic, [encodedName]);
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

    private static bool IsSupportedIntrinsicName(string intrinsicName)
    {
        if (intrinsicName == "call C#"
            || intrinsicName == "call C# ctor"
            || intrinsicName == "store_local"
            || intrinsicName == "load_local"
            || intrinsicName == "load_local_ref"
            || intrinsicName == "load_external"
            || intrinsicName == "store_external"
            || intrinsicName == "load_bool"
            || intrinsicName == "boolean_and"
            || intrinsicName == "boolean_or"
            || intrinsicName == "boolean_not")
            return true;

        return intrinsicName.StartsWith("load_", StringComparison.Ordinal)
               || intrinsicName.StartsWith("add_", StringComparison.Ordinal)
               || intrinsicName.StartsWith("sub_", StringComparison.Ordinal)
               || intrinsicName.StartsWith("mul_", StringComparison.Ordinal)
               || intrinsicName.StartsWith("div_", StringComparison.Ordinal)
               || intrinsicName.StartsWith("cmp_", StringComparison.Ordinal);
    }
}