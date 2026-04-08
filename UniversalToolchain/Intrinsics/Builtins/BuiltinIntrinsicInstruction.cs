using IntermediateRepresentationAbstractions;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;
using UniversalToolchain.Intrinsics.Legacy;

namespace UniversalToolchain.Intrinsics.Builtins;

public static class BuiltinIntrinsicInstruction
{
    private static readonly ILegacyIntrinsicDecoder LegacyDecoder = new LegacyIntrinsicDecoder();

    public static Instruction Create(IntrinsicSymbol symbol)
    {
        return Create(symbol, [], []);
    }

    public static Instruction Create(IntrinsicSymbol symbol, IReadOnlyList<object?> dataOperands)
    {
        return Create(symbol, [], dataOperands);
    }

    public static Instruction Create(IntrinsicSymbol symbol, params object?[] dataOperands)
    {
        return Create(symbol, [], dataOperands);
    }

    public static Instruction Create(IntrinsicSymbol symbol, Type runtimeType)
    {
        return Create(symbol, [IntrinsicTypeArgument.From(runtimeType)], []);
    }

    public static Instruction Create(IntrinsicSymbol symbol, Type runtimeType, IReadOnlyList<object?> dataOperands)
    {
        return Create(symbol, [IntrinsicTypeArgument.From(runtimeType)], dataOperands);
    }

    public static Instruction Create(IntrinsicSymbol symbol, IntrinsicTypeArgument typeArgument, params object?[] dataOperands)
    {
        return Create(symbol, [typeArgument], dataOperands);
    }

    public static Instruction Create(
        IntrinsicSymbol symbol,
        IReadOnlyList<IntrinsicTypeArgument> typeArguments,
        IReadOnlyList<object?> dataOperands)
    {
        return IntrinsicInstructionFactory.Create(new IntrinsicInvocation(symbol, typeArguments, dataOperands));
    }

    public static bool TryGetInvocation(Instruction instruction, out IntrinsicInvocation invocation)
    {
        if (instruction.TryGetTypedIntrinsicInvocation(out invocation))
            return true;

        return LegacyDecoder.TryDecode(instruction, out invocation);
    }

    public static bool Is(Instruction instruction, IntrinsicSymbol symbol)
    {
        return TryGetInvocation(instruction, out var invocation) && invocation.Symbol == symbol;
    }
}
