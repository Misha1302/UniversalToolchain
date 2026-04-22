using BasicCore.Contracts;
using BasicCore.Core;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Core.ServiceCollection;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public sealed class IntrinsicSemanticBootstrapPlanContractTests
{
    [Test]
    public void Build_RegisteredModulesAndProviders_ProducesDeterministicPlan()
    {
        var services = new ServiceCollection();
        services.AddCoreRuntimeInfrastructure();
        services.AddSingleton<IFrontendCoreModule, ValidProviderFrontendModule>();
        services.AddSingleton<IIRProcessingModule, ValidProviderOptimizerModule>();
        services.AddSingleton<IIntrinsicDescriptorProvider, ValidProvider>();

        var builder = new IntrinsicSemanticBootstrapPlanBuilder();
        var first = builder.Build(services);
        var second = builder.Build(services);

        Assert.Multiple(() =>
        {
            Assert.That(first.RegisteredProviderTypes, Is.EqualTo(second.RegisteredProviderTypes));
            Assert.That(first.Requirements, Is.EqualTo(second.Requirements));
            Assert.That(first.Requirements.Select(static x => x.ProviderType), Is.EqualTo(new[]
            {
                typeof(ValidProvider),
                typeof(ValidProvider)
            }));
        });
    }

    [Test]
    public void ValidatePreProvider_WhenRequirementMissingRegisteredProvider_Throws()
    {
        var services = new ServiceCollection();
        services.AddCoreRuntimeInfrastructure();
        services.AddSingleton<IFrontendCoreModule, MissingProviderFrontendModule>();

        var builder = new IntrinsicSemanticBootstrapPlanBuilder();
        var plan = builder.Build(services);
        var validator = new IntrinsicSemanticBootstrapPreProviderValidator();

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(plan, services));

        Assert.That(exception!.Message, Does.Contain(typeof(MissingProvider).FullName));
    }

    [Test]
    public void ValidateRuntime_WhenProvidersSatisfyPlan_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddCoreRuntimeInfrastructure();
        services.AddSingleton<IFrontendCoreModule, ValidProviderFrontendModule>();
        services.AddSingleton<IIntrinsicDescriptorProvider, ValidProvider>();

        var builder = new IntrinsicSemanticBootstrapPlanBuilder();
        var plan = builder.Build(services);
        var preProviderValidator = new IntrinsicSemanticBootstrapPreProviderValidator();
        preProviderValidator.Validate(plan, services);

        using var provider = services.BuildServiceProvider();
        var runtimeValidator = new IntrinsicSemanticBootstrapRuntimeValidator();

        Assert.DoesNotThrow(() => runtimeValidator.Validate(provider, plan));
    }

    private static IntrinsicSemanticDescriptor CreateDescriptor(string @namespace, string name) =>
        new()
        {
            Symbol = new IntrinsicSymbol(@namespace, name),
            Category = IntrinsicCategory.Core,
            StackRule = new NoOpStackRule(),
            ValidationRule = new NoOpValidationRule()
        };

    private sealed class MissingProvider : IIntrinsicDescriptorProvider
    {
        public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors() =>
            [CreateDescriptor("logic", "missing")];
    }

    private sealed class ValidProvider : IIntrinsicDescriptorProvider
    {
        public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors() =>
            [CreateDescriptor("logic", "valid")];
    }

    [IntrinsicDescriptorProvider(typeof(MissingProvider))]
    private sealed class MissingProviderFrontendModule : IFrontendCoreModule;

    [IntrinsicDescriptorProvider(typeof(ValidProvider))]
    private sealed class ValidProviderFrontendModule : IFrontendCoreModule;

    [IntrinsicDescriptorProvider(typeof(ValidProvider))]
    private sealed class ValidProviderOptimizerModule : IIRProcessingModule;

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
