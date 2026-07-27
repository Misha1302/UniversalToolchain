using BasicCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Core.ServiceCollection;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public sealed class IntrinsicSemanticBootstrapPlanContractTests
{
    [Test]
    public void Build_SameServiceRegistrations_ProducesDeterministicPlan()
    {
        var services = CreateServices();
        services.AddSingleton<IFrontendCoreModule, ValidProviderFrontendModule>();
        services.AddSingleton<IIntrinsicDescriptorProvider, ValidProvider>();
        services.AddSingleton<IIntrinsicDescriptorProvider>(new ValidProvider());

        var builder = new IntrinsicSemanticBootstrapPlanBuilder();
        var first = builder.Build(services);
        var second = builder.Build(services);

        Assert.That(BuildPlanSignature(first), Is.EqualTo(BuildPlanSignature(second)));
    }

    [Test]
    public void ValidatePreProvider_ImplementationTypeRegistration_ParticipatesInCoverageValidation()
    {
        var services = CreateServices();
        services.AddSingleton<IFrontendCoreModule, ValidProviderFrontendModule>();
        services.AddSingleton<IIntrinsicDescriptorProvider, ValidProvider>();

        var builder = new IntrinsicSemanticBootstrapPlanBuilder();
        var plan = builder.Build(services);
        var validator = new IntrinsicSemanticBootstrapPreProviderValidator();

        Assert.DoesNotThrow(() => validator.Validate(plan, services));
    }

    [Test]
    public void ValidatePreProvider_ImplementationInstanceRegistration_ParticipatesInCoverageValidation()
    {
        var services = CreateServices();
        services.AddSingleton<IFrontendCoreModule, ValidProviderFrontendModule>();
        services.AddSingleton<IIntrinsicDescriptorProvider>(new ValidProvider());

        var builder = new IntrinsicSemanticBootstrapPlanBuilder();
        var plan = builder.Build(services);
        var validator = new IntrinsicSemanticBootstrapPreProviderValidator();

        Assert.DoesNotThrow(() => validator.Validate(plan, services));
    }

    [Test]
    public void ValidatePreProvider_FactoryRegistration_FailsFastWithClearMessage()
    {
        var services = CreateServices();
        services.AddSingleton<IFrontendCoreModule, ValidProviderFrontendModule>();
        services.AddSingleton<IIntrinsicDescriptorProvider>(_ => new ValidProvider());

        var builder = new IntrinsicSemanticBootstrapPlanBuilder();
        var plan = builder.Build(services);
        var validator = new IntrinsicSemanticBootstrapPreProviderValidator();

        Assert.That(
            plan.ProviderRegistrations.Select(static x => x.Kind),
            Does.Contain(IntrinsicDescriptorProviderRegistrationKind.Factory));

        var exception = Assert.Throws<InvalidOperationException>(() => validator.Validate(plan, services));

        Assert.That(exception!.Message, Does.Contain("factory-based registration"));
        Assert.That(exception.Message, Does.Contain("cannot infer provider type"));
    }

    [Test]
    public void ValidateRuntime_SupportedRegistrationPaths_SucceedsAfterPreProviderValidation()
    {
        var services = CreateServices();
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

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddNeutralRuntimeInfrastructure();
        return services;
    }

    private static string BuildPlanSignature(IntrinsicSemanticBootstrapPlan plan)
    {
        return string.Join(
                   "|",
                   plan.ProviderRegistrations.Select(static x =>
                       x.RegistrationIndex + ":" + x.Kind + ":" + (x.ProviderType?.FullName ?? "<null>")))
               + "::"
               + string.Join(
                   "|",
                   plan.Requirements.Select(static x => x.ModuleType.FullName + ":" + x.ProviderType.FullName));
    }

    private static IntrinsicSemanticDescriptor CreateDescriptor(string @namespace, string name) =>
        new()
        {
            Symbol = new IntrinsicSymbol(@namespace, name),
            Category = IntrinsicCategory.Core,
            StackRule = new NoOpStackRule(),
            ValidationRule = new NoOpValidationRule()
        };

    private sealed class ValidProvider : IIntrinsicDescriptorProvider
    {
        public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors() =>
            [CreateDescriptor("logic", "valid")];
    }

    [IntrinsicDescriptorProvider(typeof(ValidProvider))]
    private sealed class ValidProviderFrontendModule : IFrontendCoreModule;

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