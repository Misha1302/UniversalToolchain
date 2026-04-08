using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;

namespace Tests.Intrinsics;

[TestFixture]
public sealed class IntrinsicSemanticStartupValidatorTests
{
    [Test]
    public void Validate_ShouldSucceed_WhenProvidersAreValid()
    {
        var validator = new IntrinsicSemanticStartupValidator();
        var providers = new IIntrinsicDescriptorProvider[]
        {
            new AlphaProvider(CreateDescriptor("math", "add")),
            new BetaProvider(CreateDescriptor("logic", "and"))
        };

        Assert.DoesNotThrow(() => validator.Validate(providers));
    }

    [Test]
    public void Validate_ShouldFail_WhenDuplicateSymbolsExist()
    {
        var validator = new IntrinsicSemanticStartupValidator();
        var providers = new IIntrinsicDescriptorProvider[]
        {
            new AlphaProvider(CreateDescriptor("math", "add")),
            new BetaProvider(CreateDescriptor("math", "add"))
        };

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(providers));

        Assert.That(exception!.Message, Does.Contain("Duplicate intrinsic semantic descriptor"));
        Assert.That(exception.Message, Does.Contain("math.add"));
    }

    private static IntrinsicSemanticDescriptor CreateDescriptor(string @namespace, string name)
    {
        return new IntrinsicSemanticDescriptor
        {
            Symbol = new IntrinsicSymbol(@namespace, name),
            Category = IntrinsicCategory.Core,
            StackRule = new NoOpStackRule(),
            ValidationRule = new NoOpValidationRule()
        };
    }

    private class FakeProvider(params IntrinsicSemanticDescriptor[] descriptors) : IIntrinsicDescriptorProvider
    {
        private readonly IReadOnlyList<IntrinsicSemanticDescriptor> _descriptors = descriptors;

        public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors()
        {
            return _descriptors;
        }
    }

    private sealed class AlphaProvider(params IntrinsicSemanticDescriptor[] descriptors) : FakeProvider(descriptors);

    private sealed class BetaProvider(params IntrinsicSemanticDescriptor[] descriptors) : FakeProvider(descriptors);

    private sealed class NoOpStackRule : IIntrinsicStackRule
    {
        public void Apply(IntrinsicInvocation invocation, List<Type> stack, IIntrinsicTypeResolutionContext context)
        {
        }
    }

    private sealed class NoOpValidationRule : IIntrinsicValidationRule
    {
        public void Validate(IntrinsicInvocation invocation, IIntrinsicTypeResolutionContext context)
        {
        }
    }
}
