using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core.Rules;
using UniversalToolchain.Intrinsics.Validation;

namespace UniversalToolchain.Intrinsics.Builtins;

public sealed class BooleanIntrinsicDescriptorProvider : IIntrinsicDescriptorProvider
{
    private readonly IReadOnlyList<IntrinsicSemanticDescriptor> _descriptors =
    [
        CreateBinaryDescriptor(BuiltinIntrinsicSymbols.Boolean.And),
        CreateBinaryDescriptor(BuiltinIntrinsicSymbols.Boolean.Or),
        new IntrinsicSemanticDescriptor
        {
            Symbol = BuiltinIntrinsicSymbols.Boolean.Not,
            Category = IntrinsicCategory.Boolean,
            StackRule = new BooleanUnaryRule(),
            ValidationRule = new NoValidationRule()
        }
    ];

    public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors()
    {
        return _descriptors;
    }

    private static IntrinsicSemanticDescriptor CreateBinaryDescriptor(IntrinsicSymbol symbol)
    {
        return new IntrinsicSemanticDescriptor
        {
            Symbol = symbol,
            Category = IntrinsicCategory.Boolean,
            StackRule = new BooleanBinaryRule(),
            ValidationRule = new NoValidationRule()
        };
    }
}
