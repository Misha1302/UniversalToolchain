using BasicCore.Core;
using BasicCore.Core.Rules;
using BasicCore.Validation;

namespace BasicCore.Builtins;

public sealed class CoreIntrinsicDescriptorProvider : IIntrinsicDescriptorProvider
{
    private readonly IReadOnlyList<IntrinsicSemanticDescriptor> _descriptors;

    public CoreIntrinsicDescriptorProvider(MethodCallTypeSemanticsResolver methodCallTypeSemanticsResolver)
    {
        methodCallTypeSemanticsResolver = methodCallTypeSemanticsResolver.ArgNotNull();

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

    public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors() => _descriptors;
}