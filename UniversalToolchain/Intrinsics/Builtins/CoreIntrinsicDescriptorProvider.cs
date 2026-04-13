using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;
using UniversalToolchain.Intrinsics.Core.Rules;
using UniversalToolchain.Intrinsics.Validation;

namespace UniversalToolchain.Intrinsics.Builtins;

public sealed class CoreIntrinsicDescriptorProvider : IIntrinsicDescriptorProvider
{
    private readonly IReadOnlyList<IntrinsicSemanticDescriptor> _descriptors;

    public CoreIntrinsicDescriptorProvider(MethodCallTypeSemanticsResolver methodCallTypeSemanticsResolver)
    {
        if (methodCallTypeSemanticsResolver == null)
            Thrower.ArgumentNull(nameof(methodCallTypeSemanticsResolver));

        _descriptors =
        [
            new IntrinsicSemanticDescriptor
            {
                Symbol = BuiltinIntrinsicSymbols.Core.CallCSharp,
                Category = IntrinsicCategory.Interop,
                StackRule = new CSharpCallStackRule(methodCallTypeSemanticsResolver),
                ValidationRule = new CompositeValidationRule(
                    new ExpectedDataOperandCountRule(1),
                    new MethodInfoOperandValidationRule())
            },
            new IntrinsicSemanticDescriptor
            {
                Symbol = BuiltinIntrinsicSymbols.Core.CallCSharpCtor,
                Category = IntrinsicCategory.Interop,
                StackRule = new CSharpCtorStackRule(),
                ValidationRule = new CompositeValidationRule(
                    new ExpectedDataOperandCountRule(1),
                    new ConstructorInfoOperandValidationRule())
            },
            new IntrinsicSemanticDescriptor
            {
                Symbol = BuiltinIntrinsicSymbols.Core.LoadExternal,
                Category = IntrinsicCategory.ExternalBinding,
                StackRule = new PushSingleTypeRule((invocation, context) => context.Resolve(invocation.TypeArguments[0])),
                ValidationRule = new CompositeValidationRule(
                    new ExpectedTypeArgumentCountRule(1))
            },
            new IntrinsicSemanticDescriptor
            {
                Symbol = BuiltinIntrinsicSymbols.Core.StoreExternal,
                Category = IntrinsicCategory.ExternalBinding,
                StackRule = new PopOneRule(),
                ValidationRule = new NoValidationRule()
            },
            new IntrinsicSemanticDescriptor
            {
                Symbol = BuiltinIntrinsicSymbols.Core.LoadConst,
                Category = IntrinsicCategory.Core,
                StackRule = new PushSingleTypeRule((invocation, context) => context.Resolve(invocation.TypeArguments[0])),
                ValidationRule = new ExpectedTypeArgumentCountRule(1)
            }
        ];
    }

    public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors()
    {
        return _descriptors;
    }
}
