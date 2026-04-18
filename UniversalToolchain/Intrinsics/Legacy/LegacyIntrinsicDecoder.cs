using ObjectExtensions;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Legacy;

/// <summary>
///     Decodes current string-based intrinsic instructions without changing existing emitters.
/// </summary>
public sealed class LegacyIntrinsicDecoder : ILegacyIntrinsicDecoder
{
    public bool TryDecode(Instruction instruction, out IntrinsicInvocation invocation)
    {
        invocation = default!;

        if (instruction.UOpCode != UOpCode.Intrinsic)
            return false;

        if (instruction.Operands.Count == 0 || instruction.Operands[0] is not string name)
            return false;

        if (TryDecodeExact(instruction, name, out invocation))
            return true;

        if (LegacyIntrinsicNameParser.TryParseArithmetic(name, out var arithmeticSymbol, out var arithmeticTypeArguments))
        {
            invocation = new IntrinsicInvocation(
                arithmeticSymbol,
                arithmeticTypeArguments,
                instruction.Operands.Skip(1).ToArray());
            return true;
        }

        if (LegacyIntrinsicNameParser.TryParseComparison(name, out var comparisonSymbol, out var comparisonTypeArguments))
        {
            invocation = new IntrinsicInvocation(
                comparisonSymbol,
                comparisonTypeArguments,
                instruction.Operands.Skip(1).ToArray());
            return true;
        }

        if (LegacyIntrinsicNameParser.TryParseLoadConst(name, out var loadConstSymbol, out var loadConstTypeArguments))
        {
            invocation = new IntrinsicInvocation(
                loadConstSymbol,
                loadConstTypeArguments,
                instruction.Operands.Skip(1).ToArray());
            return true;
        }

        return false;
    }

    private static bool TryDecodeExact(Instruction instruction, string name, out IntrinsicInvocation invocation)
    {
        invocation = default!;

        switch (name)
        {
            case "call C#":
                if (instruction.Operands.Count < 2)
                    return false;

                invocation = new IntrinsicInvocation(
                    BuiltinIntrinsicSymbols.Core.CallCSharp,
                    [],
                    [instruction.Operands[1]]);
                return true;

            case "call C# ctor":
                if (instruction.Operands.Count < 2)
                    return false;

                invocation = new IntrinsicInvocation(
                    BuiltinIntrinsicSymbols.Core.CallCSharpCtor,
                    [],
                    [instruction.Operands[1]]);
                return true;

            case "load_external":
                if (!TryGetTypeArgument(instruction, 2, out var loadExternalType))
                    return false;

                invocation = new IntrinsicInvocation(
                    BuiltinIntrinsicSymbols.Core.LoadExternal,
                    [loadExternalType],
                    [instruction.Operands[1]]);
                return true;

            case "store_external":
                if (instruction.Operands.Count < 2)
                    return false;

                invocation = new IntrinsicInvocation(
                    BuiltinIntrinsicSymbols.Core.StoreExternal,
                    [],
                    [instruction.Operands[1]]);
                return true;

            case "load_local":
                if (!TryGetTypeArgument(instruction, 2, out var loadLocalType))
                    return false;

                invocation = new IntrinsicInvocation(
                    BuiltinIntrinsicSymbols.Storage.LoadLocal,
                    [loadLocalType],
                    instruction.Operands.Skip(1).ToArray());
                return true;

            case "store_local":
                if (instruction.Operands.Count < 3)
                    return false;

                invocation = new IntrinsicInvocation(
                    BuiltinIntrinsicSymbols.Storage.StoreLocal,
                    [],
                    instruction.Operands.Skip(1).ToArray());
                return true;

            case "load_local_ref":
                if (!TryGetTypeArgument(instruction, 2, out var loadLocalRefType))
                    return false;

                invocation = new IntrinsicInvocation(
                    BuiltinIntrinsicSymbols.Storage.LoadLocalRef,
                    [loadLocalRefType],
                    instruction.Operands.Skip(1).ToArray());
                return true;

            case "boolean_and":
                invocation = new IntrinsicInvocation(
                    BuiltinIntrinsicSymbols.Boolean.And,
                    [],
                    instruction.Operands.Skip(1).ToArray());
                return true;

            case "boolean_or":
                invocation = new IntrinsicInvocation(
                    BuiltinIntrinsicSymbols.Boolean.Or,
                    [],
                    instruction.Operands.Skip(1).ToArray());
                return true;

            case "boolean_not":
                invocation = new IntrinsicInvocation(
                    BuiltinIntrinsicSymbols.Boolean.Not,
                    [],
                    instruction.Operands.Skip(1).ToArray());
                return true;

            default:
                return false;
        }
    }

    private static bool TryGetTypeArgument(Instruction instruction, int operandIndex, out IntrinsicTypeArgument typeArgument)
    {
        typeArgument = default;

        if (instruction.Operands.Count <= operandIndex || instruction.Operands[operandIndex] is not Type)
            return false;

        typeArgument = IntrinsicTypeArgument.From(instruction.Operands[operandIndex].Get<Type>());
        return true;
    }
}