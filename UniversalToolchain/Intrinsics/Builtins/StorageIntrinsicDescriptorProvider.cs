using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core.Rules;
using UniversalToolchain.Intrinsics.Validation;

namespace UniversalToolchain.Intrinsics.Builtins;

public sealed class StorageIntrinsicDescriptorProvider : IIntrinsicDescriptorProvider
{
    private readonly IReadOnlyList<IntrinsicSemanticDescriptor> _descriptors =
    [
        new IntrinsicSemanticDescriptor
        {
            Symbol = BuiltinIntrinsicSymbols.Storage.LoadLocal,
            Category = IntrinsicCategory.Storage,
            StackRule = new PushSingleTypeRule((invocation, context) => context.Resolve(invocation.TypeArguments[0])),
            ValidationRule = new ExpectedTypeArgumentCountRule(1)
        },
        new IntrinsicSemanticDescriptor
        {
            Symbol = BuiltinIntrinsicSymbols.Storage.StoreLocal,
            Category = IntrinsicCategory.Storage,
            StackRule = new PopOneRule(),
            ValidationRule = new NoValidationRule()
        },
        new IntrinsicSemanticDescriptor
        {
            Symbol = BuiltinIntrinsicSymbols.Storage.LoadLocalRef,
            Category = IntrinsicCategory.Storage,
            StackRule = new LoadLocalRefStackRule(),
            ValidationRule = new ExpectedTypeArgumentCountRule(1)
        }
    ];

    public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors()
    {
        return _descriptors;
    }
}
