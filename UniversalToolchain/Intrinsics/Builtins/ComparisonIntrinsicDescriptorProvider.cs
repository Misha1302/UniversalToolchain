using BasicCore.Core.Rules;
using BasicCore.Validation;

namespace BasicCore.Builtins;

public sealed class ComparisonIntrinsicDescriptorProvider : IIntrinsicDescriptorProvider
{
    private readonly IReadOnlyList<IntrinsicSemanticDescriptor> _descriptors =
    [
        CreateDescriptor(BuiltinIntrinsicSymbols.Comparison.Equal),
        CreateDescriptor(BuiltinIntrinsicSymbols.Comparison.NotEqual),
        CreateDescriptor(BuiltinIntrinsicSymbols.Comparison.Greater),
        CreateDescriptor(BuiltinIntrinsicSymbols.Comparison.GreaterOrEqual),
        CreateDescriptor(BuiltinIntrinsicSymbols.Comparison.Less),
        CreateDescriptor(BuiltinIntrinsicSymbols.Comparison.LessOrEqual)
    ];

    public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors() => _descriptors;

    private static IntrinsicSemanticDescriptor CreateDescriptor(IntrinsicSymbol symbol) =>
        new()
        {
            Symbol = symbol,
            Category = IntrinsicCategory.Comparison,
            StackRule = new BinaryComparisonRule(),
            ValidationRule = new ExpectedTypeArgumentCountRule(1)
        };
}