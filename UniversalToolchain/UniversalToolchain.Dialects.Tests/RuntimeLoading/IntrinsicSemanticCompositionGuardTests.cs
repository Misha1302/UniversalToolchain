using BasicCore.Contracts;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public sealed class IntrinsicSemanticCompositionGuardTests
{
    [Test]
    public void RuntimeFactory_ShouldFailFast_WhenDuplicateIntrinsicSymbolsAreRegistered()
    {
        var factory = new WistDialectServiceProviderFactory([]);
        var configuration = new WistDialectExecutionConfiguration(
            "DuplicateIntrinsicGuard",
            [typeof(DuplicateAlphaFrontendModule), typeof(DuplicateBetaFrontendModule)],
            [],
            [],
            [],
            []);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.Create(configuration));

        Assert.That(exception!.Message, Does.Contain("Intrinsic symbol"));
        Assert.That(exception.Message, Does.Contain("math.add"));
    }

    private static IntrinsicSemanticDescriptor CreateDescriptor(string @namespace, string name) =>
        new()
        {
            Symbol = new IntrinsicSymbol(@namespace, name),
            Category = IntrinsicCategory.Core,
            StackRule = new NoOpStackRule(),
            ValidationRule = new NoOpValidationRule()
        };

    [IntrinsicDescriptorProvider(typeof(DuplicateAlphaIntrinsicDescriptorProvider))]
    private sealed class DuplicateAlphaFrontendModule : IFrontendCoreModule;

    [IntrinsicDescriptorProvider(typeof(DuplicateBetaIntrinsicDescriptorProvider))]
    private sealed class DuplicateBetaFrontendModule : IFrontendCoreModule;

    private sealed class DuplicateAlphaIntrinsicDescriptorProvider : IIntrinsicDescriptorProvider
    {
        public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors() =>
            [CreateDescriptor("math", "add")];
    }

    private sealed class DuplicateBetaIntrinsicDescriptorProvider : IIntrinsicDescriptorProvider
    {
        public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors() =>
            [CreateDescriptor("math", "add")];
    }

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