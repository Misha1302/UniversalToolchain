using BasicCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.ServiceCollection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public sealed class IntrinsicSemanticStartupValidationRuntimeTests
{
    [Test]
    public void RuntimeFactory_ShouldFail_WhenModuleDeclaresInvalidProviderType()
    {
        var factory = new WistDialectServiceProviderFactory([]);
        var configuration = new WistDialectExecutionConfiguration(
            "InvalidProviderType",
            [typeof(InvalidProviderTypeFrontendModule)],
            [],
            [],
            [],
            []);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.Create(configuration));

        Assert.That(exception!.Message, Does.Contain(typeof(InvalidProviderTypeFrontendModule).FullName));
        Assert.That(exception.Message, Does.Contain(typeof(NotAProvider).FullName));
        Assert.That(exception.Message, Does.Contain(nameof(IIntrinsicDescriptorProvider)));
    }

    [Test]
    public void RuntimeStartupValidation_ShouldFail_WhenAttributedModuleProviderIsMissing()
    {
        var services = new ServiceCollection();
        services.AddCoreRuntimeInfrastructure();
        services.AddSingleton<IFrontendCoreModule, MissingProviderFrontendModule>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IntrinsicSemanticStartupValidator>();
        var providers = provider.GetServices<IIntrinsicDescriptorProvider>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            validator.Validate(providers, [(typeof(MissingProviderFrontendModule), typeof(MissingProvider))]));

        Assert.That(exception!.Message, Does.Contain(typeof(MissingProviderFrontendModule).FullName));
        Assert.That(exception.Message, Does.Contain(typeof(MissingProvider).FullName));
        Assert.That(exception.Message, Does.Contain("not registered"));
    }

    [Test]
    public void RuntimeFactory_ShouldSucceed_WhenIntrinsicProvidersCoverSelectedModules()
    {
        var factory = new WistDialectServiceProviderFactory([]);
        var configuration = new WistDialectExecutionConfiguration(
            "ValidProviderCoverage",
            [typeof(ValidProviderFrontendModule)],
            [],
            [],
            [],
            []);

        Assert.DoesNotThrow(() =>
        {
            _ = factory.Create(configuration);
        });
    }

    [Test]
    public void RuntimeFactory_ShouldReuseSingletonProviderDependencies_AfterStartupValidation()
    {
        CountingDependency.Reset();

        var factory = new WistDialectServiceProviderFactory([new CountingBackendRegistrar()]);
        var configuration = new WistDialectExecutionConfiguration(
            "CountingProvider",
            [],
            [],
            [],
            [new DialectBackendRuntimeConfiguration(
                new RuntimeBackendDescriptor(new DialectBackendId("counting"), typeof(CountingBackendRegistrar)),
                [],
                [],
                [],
                false)],
            [new RuntimeBackendDescriptor(new DialectBackendId("counting"), typeof(CountingBackendRegistrar))]);

        var provider = factory.Create(configuration);
        using var providerLifetime = provider as IDisposable;
        _ = provider.GetServices<IIntrinsicDescriptorProvider>()
            .Single(static x => x.GetType() == typeof(CountingDescriptorProvider));

        Assert.That(CountingDependency.CreationCount, Is.EqualTo(1));
    }

    private sealed class NotAProvider;

    private sealed class MissingProvider : IIntrinsicDescriptorProvider
    {
        public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors()
        {
            return [CreateDescriptor("logic", "missing")];
        }
    }

    private sealed class ValidProvider : IIntrinsicDescriptorProvider
    {
        public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors()
        {
            return [CreateDescriptor("logic", "present")];
        }
    }

    private sealed class CountingDependency
    {
        private static int _creationCount;

        public CountingDependency()
        {
            _creationCount++;
        }

        public static int CreationCount => _creationCount;

        public static void Reset()
        {
            _creationCount = 0;
        }
    }

    private sealed class CountingDescriptorProvider(CountingDependency dependency) : IIntrinsicDescriptorProvider
    {
        public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors()
        {
            ArgumentNullException.ThrowIfNull(dependency);
            return [CreateDescriptor("counting", "provider")];
        }
    }

    private sealed class CountingBackendRegistrar : IDialectBackendRuntimeRegistrar
    {
        public DialectBackendId BackendId { get; } = new("counting");

        public IReadOnlyList<string> SupportedIntrinsics => [];

        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
            services.AddSingleton<CountingDependency>();
            services.AddSingleton<IIntrinsicDescriptorProvider, CountingDescriptorProvider>();
        }
    }

    [IntrinsicDescriptorProvider(typeof(NotAProvider))]
    private sealed class InvalidProviderTypeFrontendModule : IFrontendCoreModule;

    [IntrinsicDescriptorProvider(typeof(MissingProvider))]
    private sealed class MissingProviderFrontendModule : IFrontendCoreModule;

    [IntrinsicDescriptorProvider(typeof(ValidProvider))]
    private sealed class ValidProviderFrontendModule : IFrontendCoreModule;

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
