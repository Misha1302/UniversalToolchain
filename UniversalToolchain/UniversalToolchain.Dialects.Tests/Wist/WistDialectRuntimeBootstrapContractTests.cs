using BasicCore.LexerWrapper;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist;

public class WistDialectRuntimeBootstrapContractTests
{
    [Test]
    public void AddWistDialectServices_ShouldRegisterCanonicalRuntimeInfrastructure()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        Assert.Multiple(() =>
        {
            Assert.That(services.Any(static x => x.ServiceType == typeof(SelectedRuntimePlanResolver)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeComponentCatalog)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeComponentTypeLoader)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(WistDialectExecutionWorkflow)), Is.True);
        });
    }

    [Test]
    public void AddWistDialectServices_ShouldNotRegisterLegacyCompositionServices()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        var serviceTypes = services.Select(static x => x.ServiceType.FullName ?? string.Empty).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(serviceTypes.Any(static x => x.Contains("AutoRegistration", StringComparison.Ordinal)), Is.False);
            Assert.That(serviceTypes.Any(static x => x.Contains("Legacy", StringComparison.Ordinal)), Is.False);
            Assert.That(serviceTypes.Any(static x => x.Contains("WistOptions", StringComparison.Ordinal)), Is.False);
        });
    }

    [Test]
    public void WistDialectServicesRegistrar_ShouldNotHardcodeConcreteBackends()
    {
        var services = new ServiceCollection();
        var registrar = new WistDialectServicesRegistrar();

        registrar.Register(services);

        Assert.That(services.Where(static x => x.ServiceType == typeof(IDialectBackendRuntimeRegistrar)), Is.Empty);
    }

    [Test]
    public void WistDialectServiceProviderFactory_ShouldUseNeutralCoreBootstrap()
    {
        var factory = new WistDialectServiceProviderFactory([new NoopRegistrar("interpreter")]);
        var config = new WistDialectExecutionConfiguration(
            "Demo",
            [],
            [],
            [],
            [new DialectBackendRuntimeConfiguration(new RuntimeBackendDescriptor(new DialectBackendId("interpreter"), typeof(NoopRegistrar), ["vm"]), [], [], false)],
            [new RuntimeBackendDescriptor(new DialectBackendId("interpreter"), typeof(NoopRegistrar), ["vm"])]);

        var provider = factory.Create(config);

        Assert.That(provider.GetService<Func<ILexer>>(), Is.Not.Null);
        (provider as IDisposable)?.Dispose();
    }

    [Test]
    public void CreateHost_ShouldFailClearly_IfRequestedBackendRegistrarIsMissing()
    {
        var factory = new WistDialectServiceProviderFactory([]);
        var config = new WistDialectExecutionConfiguration(
            "Demo",
            [],
            [],
            [],
            [new DialectBackendRuntimeConfiguration(new RuntimeBackendDescriptor(new DialectBackendId("interpreter"), typeof(object), []), [], [], false)],
            [new RuntimeBackendDescriptor(new DialectBackendId("interpreter"), typeof(object), [])]);

        var ex = Assert.Throws<InvalidOperationException>(() => factory.Create(config));

        Assert.That(ex!.Message, Does.Contain("No backend runtime registrar is registered for backend 'interpreter'"));
    }

    [Test]
    public void CreateHost_ShouldProduceStableConfiguration_AcrossRepeatedCalls()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect Stable\nuse Arithmetic,Numbers\nbackend interpreter,compiler", "inline");

        var signatures = new List<string>();
        for (var i = 0; i < 30; i++)
        {
            using var host = workflow.CreateHost(composition);
            signatures.Add(DescribeHost(host));
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
    }

    private static string DescribeHost(WistDialectExecutionHost host)
    {
        return string.Join("|", host.Configuration.FrontendModules.Select(static x => x.FullName))
               + "::"
               + string.Join("|", host.Configuration.IrModules.Select(static x => x.FullName))
               + "::"
               + string.Join("|", host.Configuration.BackendConfigurations.Select(static x => x.BackendDescriptor.CanonicalId));
    }

    private sealed class NoopRegistrar(string backendId) : IDialectBackendRuntimeRegistrar
    {
        public DialectBackendId BackendId { get; } = new(backendId);
        public IReadOnlyList<string> SupportedIntrinsics => [];
        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
            services.AddSingleton(typeof(object), new object());
        }
    }

}
