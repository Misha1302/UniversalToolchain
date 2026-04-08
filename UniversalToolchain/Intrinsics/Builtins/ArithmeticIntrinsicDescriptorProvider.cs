using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core.Rules;
using UniversalToolchain.Intrinsics.Validation;

namespace UniversalToolchain.Intrinsics.Builtins;

public sealed class ArithmeticIntrinsicDescriptorProvider : IIntrinsicDescriptorProvider
{
    private readonly IReadOnlyList<IntrinsicSemanticDescriptor> _descriptors =
    [
        CreateDescriptor(BuiltinIntrinsicSymbols.Arithmetic.Add),
        CreateDescriptor(BuiltinIntrinsicSymbols.Arithmetic.Subtract),
        CreateDescriptor(BuiltinIntrinsicSymbols.Arithmetic.Multiply),
        CreateDescriptor(BuiltinIntrinsicSymbols.Arithmetic.Divide)
    ];

    public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors()
    {
        return _descriptors;
    }

    private static IntrinsicSemanticDescriptor CreateDescriptor(IntrinsicSymbol symbol)
    {
        return new IntrinsicSemanticDescriptor
        {
            Symbol = symbol,
            Category = IntrinsicCategory.Arithmetic,
            StackRule = new BinarySameTypeResultRule(),
            ValidationRule = new ExpectedTypeArgumentCountRule(1)
        };
    }
}
