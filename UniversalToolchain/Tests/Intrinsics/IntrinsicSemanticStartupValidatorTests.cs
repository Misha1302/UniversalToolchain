namespace Tests.Intrinsics;

[TestFixture]
public sealed class IntrinsicSemanticStartupValidatorTests
{
    [Test]
    public void Validate_ShouldFail_WhenDuplicateProviderTypeIsRegistered()
    {
        var validator = new IntrinsicSemanticStartupValidator();
        var providers = new IIntrinsicDescriptorProvider[]
        {
            new DuplicateProvider(CreateDescriptor("math", "add")),
            new DuplicateProvider(CreateDescriptor("logic", "and"))
        };

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(providers));

        Assert.That(exception!.Message, Does.Contain(typeof(DuplicateProvider).FullName));
        Assert.That(exception.Message, Does.Contain("registered 2 times"));
    }

    [Test]
    public void Validate_ShouldFail_WhenProviderReturnsInvalidDescriptor()
    {
        var validator = new IntrinsicSemanticStartupValidator();
        var providers = new IIntrinsicDescriptorProvider[]
        {
            new InvalidDescriptorProvider(new IntrinsicSemanticDescriptor
            {
                Symbol = default,
                Category = IntrinsicCategory.Core,
                StackRule = null!,
                ValidationRule = new NoOpValidationRule()
            })
        };

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(providers));

        Assert.That(exception!.Message, Does.Contain(typeof(InvalidDescriptorProvider).FullName));
        Assert.That(exception.Message, Does.Contain("default symbol"));
    }

    [Test]
    public void Validate_ShouldFail_WhenAttributedModuleProviderIsMissing()
    {
        var validator = new IntrinsicSemanticStartupValidator();
        var providers = Array.Empty<IIntrinsicDescriptorProvider>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            validator.Validate(providers, [(typeof(MissingProviderFrontendModule), typeof(MissingProvider))]));

        Assert.That(exception!.Message, Does.Contain(typeof(MissingProviderFrontendModule).FullName));
        Assert.That(exception.Message, Does.Contain(typeof(MissingProvider).FullName));
        Assert.That(exception.Message, Does.Contain("not registered"));
    }

    [Test]
    public void Validate_ShouldSucceed_WhenProvidersAndCoverageAreValid()
    {
        var validator = new IntrinsicSemanticStartupValidator();
        var providers = new IIntrinsicDescriptorProvider[]
        {
            new ValidProvider(CreateDescriptor("math", "add"))
        };

        var result = validator.Validate(providers, [(typeof(ValidFrontendModule), typeof(ValidProvider))]);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    private static IntrinsicSemanticDescriptor CreateDescriptor(string @namespace, string name) =>
        new()
        {
            Symbol = new IntrinsicSymbol(@namespace, name),
            Category = IntrinsicCategory.Core,
            StackRule = new NoOpStackRule(),
            ValidationRule = new NoOpValidationRule()
        };

    private class FakeProvider(params IntrinsicSemanticDescriptor[] descriptors) : IIntrinsicDescriptorProvider
    {
        private readonly IReadOnlyList<IntrinsicSemanticDescriptor> _descriptors = descriptors;

        public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors() => _descriptors;
    }

    private sealed class DuplicateProvider(params IntrinsicSemanticDescriptor[] descriptors) : FakeProvider(descriptors);

    private sealed class InvalidDescriptorProvider(params IntrinsicSemanticDescriptor[] descriptors) : FakeProvider(descriptors);

    private sealed class MissingProvider : IIntrinsicDescriptorProvider
    {
        public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors() =>
            [CreateDescriptor("logic", "missing")];
    }

    private sealed class ValidProvider(params IntrinsicSemanticDescriptor[] descriptors) : FakeProvider(descriptors);

    [IntrinsicDescriptorProvider(typeof(MissingProvider))]
    private sealed class MissingProviderFrontendModule : IFrontendCoreModule;

    [IntrinsicDescriptorProvider(typeof(ValidProvider))]
    private sealed class ValidFrontendModule : IFrontendCoreModule;

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