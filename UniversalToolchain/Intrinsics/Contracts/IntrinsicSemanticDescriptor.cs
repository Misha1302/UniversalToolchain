namespace UniversalToolchain.Intrinsics.Contracts;

public sealed class IntrinsicSemanticDescriptor
{
    public required IntrinsicSymbol Symbol { get; init; }

    public required IntrinsicCategory Category { get; init; }

    public required IIntrinsicStackRule StackRule { get; init; }

    public required IIntrinsicValidationRule ValidationRule { get; init; }

    public string? Description { get; init; }
}
