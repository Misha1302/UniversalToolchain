namespace Tests.Intrinsics;

[TestFixture]
public sealed class IntrinsicCatalogBuilderTests
{
    [Test]
    public void Build_ShouldCreateCatalog_WhenProvidersAreValid()
    {
        var builder = new IntrinsicCatalogBuilder();
        var providers = new IIntrinsicDescriptorProvider[]
        {
            new FakeProvider(
                CreateDescriptor("math", "add"),
                CreateDescriptor("math", "sub"))
        };

        var catalog = builder.Build(providers);

        Assert.That(catalog.All.Select(x => x.Symbol), Is.EqualTo(new[]
        {
            new IntrinsicSymbol("math", "add"),
            new IntrinsicSymbol("math", "sub")
        }));
        Assert.That(catalog.Resolve(new IntrinsicSymbol("math", "add")).Category, Is.EqualTo(IntrinsicCategory.Core));
    }

    [Test]
    public void Build_ShouldFail_WhenDuplicateSymbolsArePresent()
    {
        var builder = new IntrinsicCatalogBuilder();
        var providers = new IIntrinsicDescriptorProvider[]
        {
            new FakeProvider(CreateDescriptor("math", "add")),
            new FakeProvider(CreateDescriptor("math", "add"))
        };

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build(providers));

        Assert.That(exception!.Message, Does.Contain("Duplicate intrinsic semantic descriptor"));
        Assert.That(exception.Message, Does.Contain("math.add"));
    }

    [Test]
    public void Build_ShouldProduceDeterministicCatalog_WhenProviderOrderDiffers()
    {
        var builder = new IntrinsicCatalogBuilder();
        var firstProviders = new IIntrinsicDescriptorProvider[]
        {
            new ZetaProvider(CreateDescriptor("math", "sub"), CreateDescriptor("logic", "or")),
            new AlphaProvider(CreateDescriptor("math", "add"), CreateDescriptor("logic", "and"))
        };
        var secondProviders = new IIntrinsicDescriptorProvider[]
        {
            new AlphaProvider(CreateDescriptor("logic", "and"), CreateDescriptor("math", "add")),
            new ZetaProvider(CreateDescriptor("logic", "or"), CreateDescriptor("math", "sub"))
        };

        var firstCatalog = builder.Build(firstProviders);
        var secondCatalog = builder.Build(secondProviders);

        Assert.That(firstCatalog.All.Select(x => x.Symbol), Is.EqualTo(secondCatalog.All.Select(x => x.Symbol)));
        Assert.That(firstCatalog.All.Select(x => x.Symbol), Is.EqualTo(new[]
        {
            new IntrinsicSymbol("logic", "and"),
            new IntrinsicSymbol("math", "add"),
            new IntrinsicSymbol("logic", "or"),
            new IntrinsicSymbol("math", "sub")
        }));
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

    private sealed class AlphaProvider(params IntrinsicSemanticDescriptor[] descriptors) : FakeProvider(descriptors);

    private sealed class ZetaProvider(params IntrinsicSemanticDescriptor[] descriptors) : FakeProvider(descriptors);

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