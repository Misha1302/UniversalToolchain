namespace UniversalToolchain.Intrinsics.Contracts;

public sealed record IntrinsicInvocation(
    IntrinsicSymbol Symbol,
    IReadOnlyList<IntrinsicTypeArgument> TypeArguments,
    IReadOnlyList<object?> DataOperands);
