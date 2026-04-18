using AbstractIrConverters;
using BasicCore.LexerWrapper;
using BytecodeDynamicMethodsCompiler.Compilers;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist;

public class WistDialectRuntimeBootstrapContractTests
{
    [Test]
    public void AddWistDialectCoreServices_ShouldRegisterWorkflowWithoutFullRuntimePolicy()
    {
        var services = new ServiceCollection();
        services.AddWistDialectCoreServices();

        Assert.Multiple(() =>
        {
            Assert.That(services.Any(static x => x.ServiceType == typeof(WistDialectExecutionWorkflow)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(SelectedRuntimePlanResolver)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeComponentCatalog)), Is.False);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeComponentTypeLoader)), Is.False);
        });
    }

    [Test]
    public void AddFileSystemRuntimeCatalogServices_ShouldRegisterCatalogArtifactsOnly()
    {
        var services = new ServiceCollection();
        services.AddFileSystemRuntimeCatalogServices();

        Assert.Multiple(() =>
        {
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeManifestFileLocator)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeManifestSerializer)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeComponentCatalog)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeComponentTypeLoader)), Is.False);
            Assert.That(services.Any(static x => x.ServiceType == typeof(WistDialectExecutionWorkflow)), Is.False);
        });
    }

    [Test]
    public void AddReflectionRuntimeResolutionServices_ShouldRegisterResolutionArtifactsOnly()
    {
        var services = new ServiceCollection();
        services.AddReflectionRuntimeResolutionServices();

        Assert.Multiple(() =>
        {
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeAssemblyLocator)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeAssemblyLoadStrategy)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeComponentResolver)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeComponentTypeLoader)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeKnownBackendsProvider)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(WistDialectExecutionWorkflow)), Is.False);
        });
    }

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
    public void AddWistDialectServices_ShouldMatchCanonicalCompositionOfBlocks()
    {
        var wrapperServices = new ServiceCollection();
        wrapperServices.AddWistDialectServices();

        var blockServices = new ServiceCollection();
        blockServices
            .AddWistDialectCoreServices()
            .AddFileSystemRuntimeCatalogServices()
            .AddReflectionRuntimeResolutionServices();

        Assert.That(BuildServiceSignatures(wrapperServices), Is.EqualTo(BuildServiceSignatures(blockServices)));
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
    public void WistDialectServicesRegistrar_ShouldKeepBackendRegistrationExplicitAndOptIn()
    {
        var services = new ServiceCollection();
        var registrar = new WistDialectServicesRegistrar();

        registrar.Register(services);

        Assert.Multiple(() =>
        {
            Assert.That(services.Where(static x => x.ServiceType == typeof(IDialectBackendRuntimeRegistrar)), Is.Empty);
            Assert.That(services.Any(static x => x.ServiceType == typeof(WistDialectExecutionWorkflow)), Is.True);
        });
    }

    [Test]
    public void AddWistDialectServices_WithoutExplicitBackends_ShouldNotAllowHostCreationForBackendedDialect()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect MissingBackends\nuse Arithmetic\nbackend interpreter", "missing-backends");

        Assert.That(composition.IsSuccess, Is.True, composition.ToDeterministicText());

        var ex = Assert.Throws<InvalidOperationException>(() => workflow.CreateHost(composition));
        Assert.That(ex!.Message, Does.Contain("No backend runtime registrar is registered for backend 'interpreter'"));
    }

    [Test]
    public void ExplicitBackendRegistration_ShouldEnableOnlyThoseBackendsThatWereAdded()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var interpreterOnly = workflow.ComposeText("dialect InterpreterOnly\nuse Arithmetic\nbackend interpreter", "interpreter-only");
        var compilerRequested = workflow.ComposeText("dialect NeedsCompiler\nuse Arithmetic\nbackend compiler", "needs-compiler");

        Assert.That(interpreterOnly.IsSuccess, Is.True, interpreterOnly.ToDeterministicText());
        using var interpreterHost = workflow.CreateHost(interpreterOnly);

        Assert.That(compilerRequested.IsSuccess, Is.True, compilerRequested.ToDeterministicText());
        var ex = Assert.Throws<InvalidOperationException>(() => workflow.CreateHost(compilerRequested));
        Assert.That(ex!.Message, Does.Contain("No backend runtime registrar is registered for backend 'cil'"));
    }

    [Test]
    public void CanonicalBootstrap_ShouldRemainStableAcrossRepeatedServiceProviderBuilds()
    {
        var signatures = new List<string>();
        for (var i = 0; i < 30; i++)
        {
            var services = new ServiceCollection();
            services.AddWistDialectServices();
            services.AddWistCilBackend();
            services.AddWistInterpreterBackend();

            using var provider = services.BuildServiceProvider();
            var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
            var composition = workflow.ComposeText("dialect Stable\nuse Arithmetic,Numbers\nbackend interpreter,compiler", $"stable-{i}");
            Assert.That(composition.IsSuccess, Is.True, composition.ToDeterministicText());

            using var host = workflow.CreateHost(composition);
            signatures.Add(WistDialectTestInfrastructure.BuildHostSignature(host));
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
    }

    [Test]
    public void WistDialectServiceProviderFactory_ShouldRegisterFrontendDefaultsWithoutBackendDefaults()
    {
        var factory = new WistDialectServiceProviderFactory([new NoopRegistrar("interpreter")]);
        var config = new WistDialectExecutionConfiguration(
            "Demo",
            [],
            [],
            [],
            [new DialectBackendRuntimeConfiguration(new RuntimeBackendDescriptor(new DialectBackendId("interpreter"), typeof(NoopRegistrar), ["vm"]), [], [], [], false)],
            [new RuntimeBackendDescriptor(new DialectBackendId("interpreter"), typeof(NoopRegistrar), ["vm"])]);

        var provider = factory.Create(config);

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetService<Func<ILexer>>(), Is.Not.Null);
            Assert.That(provider.GetService<AbstractMethodsCompilerImpl>(), Is.Null);
            Assert.That(provider.GetService<AbstractIrToAbstractIrStub>(), Is.Null);
        });
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
            [new DialectBackendRuntimeConfiguration(new RuntimeBackendDescriptor(new DialectBackendId("interpreter"), typeof(object), []), [], [], [], false)],
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
            signatures.Add(WistDialectTestInfrastructure.BuildHostSignature(host));
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
    }

    private static IReadOnlyList<ServiceRegistrationSignature> BuildServiceSignatures(IServiceCollection services)
    {
        return services
            .Select(static service => new ServiceRegistrationSignature(
                service.ServiceType,
                service.ImplementationType,
                service.Lifetime))
            .ToArray();
    }

    private readonly record struct ServiceRegistrationSignature(
        Type ServiceType,
        Type? ImplementationType,
        ServiceLifetime Lifetime);

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