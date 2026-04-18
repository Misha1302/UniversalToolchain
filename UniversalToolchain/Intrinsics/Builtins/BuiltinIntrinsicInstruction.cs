using BasicCore.Core;
using BasicCore.Legacy;

namespace BasicCore.Builtins;

public static class BuiltinIntrinsicInstruction
{
    private static readonly ILegacyIntrinsicDecoder LegacyDecoder = new LegacyIntrinsicDecoder();

    public static Instruction Create(IntrinsicSymbol symbol) => Create(symbol, [], []);

    public static Instruction Create(IntrinsicSymbol symbol, IReadOnlyList<object?> dataOperands) => Create(symbol, [], dataOperands);

    public static Instruction Create(IntrinsicSymbol symbol, params object?[] dataOperands) => Create(symbol, [], dataOperands);

    public static Instruction Create(IntrinsicSymbol symbol, Type runtimeType) => Create(symbol, [IntrinsicTypeArgument.From(runtimeType)], []);

    public static Instruction Create(IntrinsicSymbol symbol, Type runtimeType, IReadOnlyList<object?> dataOperands) => Create(symbol, [IntrinsicTypeArgument.From(runtimeType)], dataOperands);

    public static Instruction Create(IntrinsicSymbol symbol, IntrinsicTypeArgument typeArgument, params object?[] dataOperands) => Create(symbol, [typeArgument], dataOperands);

    public static Instruction Create(
        IntrinsicSymbol symbol,
        IReadOnlyList<IntrinsicTypeArgument> typeArguments,
        IReadOnlyList<object?> dataOperands) =>
        IntrinsicInstructionFactory.Create(new IntrinsicInvocation(symbol, typeArguments, dataOperands));

    public static bool TryGetInvocation(Instruction instruction, out IntrinsicInvocation invocation)
    {
        if (instruction.TryGetTypedIntrinsicInvocation(out invocation))
            return true;

        return LegacyDecoder.TryDecode(instruction, out invocation);
    }

    public static bool Is(Instruction instruction, IntrinsicSymbol symbol) => TryGetInvocation(instruction, out var invocation) && invocation.Symbol == symbol;
}