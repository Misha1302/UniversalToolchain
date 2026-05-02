using BasicCore.Contracts;
using BasicCore.Core;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.ServiceCollection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public sealed class IntrinsicSemanticStartupValidationRuntimeTests
{
    [Test]
    public void RuntimeFactory_ShouldFail_WhenModuleDeclaresInvalidProviderType()
    {
        var factory = CreateFactory([]);
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
        var factory = CreateFactory([]);
        var configuration = new WistDialectExecutionConfiguration(
            "ValidProviderCoverage",
            [typeof(ValidProviderFrontendModule)],
            [],
            [],
            [],
            []);

        Assert.DoesNotThrow(() => { _ = factory.Create(configuration); });
    }

    [Test]
    public void RuntimeFactory_ShouldReuseSingletonProviderDependencies_AfterStartupValidation()
    {
        CountingDependency.Reset();

        var factory = CreateFactory([new CountingBackendRegistrar()]);
        var backendEntry = BackendEntry("counting", typeof(CountingBackendRegistrar));
        var configuration = new WistDialectExecutionConfiguration(
            "CountingProvider",
            [],
            [],
            [],
            [
                new DialectBackendRuntimeConfiguration(
                    backendEntry,
                    new RuntimeBackendDescriptor(new DialectBackendId("counting"), typeof(CountingBackendRegistrar)),
                    [],
                    [],
                    [],
                    false)
            ],
            [new RuntimeBackendDescriptor(new DialectBackendId("counting"), typeof(CountingBackendRegistrar))]);

        var provider = factory.Create(configuration);
        using var providerLifetime = provider as IDisposable;
        _ = provider.GetServices<IIntrinsicDescriptorProvider>()
            .Single(static x => x.GetType() == typeof(CountingDescriptorProvider));

        Assert.That(CountingDependency.CreationCount, Is.EqualTo(1));
    }


    private static WistDialectServiceProviderFactory CreateFactory(IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars) =>
        new(
            new StaticBackendRegistrarResolver(backendRegistrars),
            new IntrinsicSemanticBootstrapPlanBuilder(),
            new IntrinsicSemanticBootstrapPreProviderValidator(),
            new IntrinsicSemanticBootstrapRuntimeValidator());

    private static RuntimeComponentManifestEntry BackendEntry(string alias, Type registrarType)
        => new(
            RuntimeComponentKind.Backend,
            alias,
            [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.Backend, alias),
            registrarType.Assembly.GetName().Name!,
            new RuntimeComponentActivationInfo(typeof(object).FullName!, registrarType.FullName));

    private static IntrinsicSemanticDescriptor CreateDescriptor(string @namespace, string name) =>
        new()
        {
            Symbol = new IntrinsicSymbol(@namespace, name),
            Category = IntrinsicCategory.Core,
            StackRule = new NoOpStackRule(),
            ValidationRule = new NoOpValidationRule()
        };

    private sealed class NotAProvider;

    private sealed class MissingProvider : IIntrinsicDescriptorProvider
    {
        public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors() =>
            [CreateDescriptor("logic", "missing")];
    }

    private sealed class ValidProvider : IIntrinsicDescriptorProvider
    {
        public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors() =>
            [CreateDescriptor("logic", "present")];
    }

    private sealed class CountingDependency
    {
        public CountingDependency()
        {
            CreationCount++;
        }

        public static int CreationCount { get; private set; }

        public static void Reset()
        {
            CreationCount = 0;
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

    private sealed class StaticBackendRegistrarResolver(IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars) : IRuntimeBackendRegistrarResolver
    {
        private readonly IReadOnlyDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar> _registrarsById = backendRegistrars.ToDictionary(
            static x => x.BackendId,
            static x => x);

        public IDialectBackendRuntimeRegistrar Resolve(RuntimeComponentManifestEntry backendEntry) => _registrarsById[new DialectBackendId(backendEntry.CanonicalAlias)];
    }
}