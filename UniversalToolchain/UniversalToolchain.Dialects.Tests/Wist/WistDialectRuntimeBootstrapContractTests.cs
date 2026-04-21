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

        var registeredTypeNames = BuildRegisteredTypeNames(services);

        Assert.Multiple(() =>
        {
            Assert.That(registeredTypeNames.Any(static x => x.Contains("AutoRegistration", StringComparison.Ordinal)), Is.False);
            Assert.That(registeredTypeNames.Any(static x => x.Contains("Legacy", StringComparison.Ordinal)), Is.False);
            Assert.That(registeredTypeNames.Any(static x => x.Contains("WistOptions", StringComparison.Ordinal)), Is.False);
        });
    }

    [Test]
    public void AddWistDialectServices_CanonicalBootstrap_DoesNotRegisterEagerAutoRegistrationConcepts()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        var registeredTypeNames = BuildRegisteredTypeNames(services);

        Assert.Multiple(() =>
        {
            Assert.That(registeredTypeNames.Any(static x => x.Contains("AutoRegisterServiceAttribute", StringComparison.Ordinal)), Is.False);
            Assert.That(registeredTypeNames.Any(static x => x.Contains("AutoRegister", StringComparison.Ordinal)), Is.False);
            Assert.That(registeredTypeNames.Any(static x => x.Contains("AutoRegistration", StringComparison.Ordinal)), Is.False);
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
    public void ComposeText_CanonicalPath_DoesNotCreateHostImplicitly()
    {
        var registrar = new CountingRegistrar("interpreter");
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddSingleton<IDialectBackendRuntimeRegistrar>(registrar);

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var composition = workflow.ComposeText("dialect ComposeOnly\nuse Arithmetic,Numbers\nbackend interpreter", "compose-only");

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True, UniversalToolchain.Dialects.Integration.DialectCompositionExplanationFormatter.FormatDeterministic(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationProjector.Project(composition)));
            Assert.That(composition.RuntimeSelection, Is.InstanceOf<SelectedRuntimePlan>());
            Assert.That(registrar.RegisterRuntimeCallCount, Is.EqualTo(0));
        });

        using var host = workflow.CreateHost(composition);

        Assert.That(registrar.RegisterRuntimeCallCount, Is.EqualTo(1));
    }

    [Test]
    public void AddWistDialectServices_WithoutExplicitBackends_ShouldNotAllowHostCreationForBackendedDialect()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect MissingBackends\nuse Arithmetic\nbackend interpreter", "missing-backends");

        Assert.That(composition.IsSuccess, Is.True, UniversalToolchain.Dialects.Integration.DialectCompositionExplanationFormatter.FormatDeterministic(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationProjector.Project(composition)));

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

        Assert.That(interpreterOnly.IsSuccess, Is.True, UniversalToolchain.Dialects.Integration.DialectCompositionExplanationFormatter.FormatDeterministic(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationProjector.Project(interpreterOnly)));
        using var interpreterHost = workflow.CreateHost(interpreterOnly);

        Assert.That(compilerRequested.IsSuccess, Is.True, UniversalToolchain.Dialects.Integration.DialectCompositionExplanationFormatter.FormatDeterministic(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationProjector.Project(compilerRequested)));
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
            Assert.That(composition.IsSuccess, Is.True, UniversalToolchain.Dialects.Integration.DialectCompositionExplanationFormatter.FormatDeterministic(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationProjector.Project(composition)));

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
    public void ComposeCreateRun_CanonicalPath_RepeatedCycles_ShouldKeepDeterministicCompositeSignature()
    {
        using var provider = WistDialectTestInfrastructure.CreateProviderWithExplicitBackends();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var signatures = new List<string>();
        for (var i = 0; i < 30; i++)
        {
            var composition = workflow.ComposeText(
                "dialect Stable\nuse Arithmetic,Numbers,Whitespaces\nenable LocalVariablesOptimization\nbackend interpreter,compiler",
                $"stable-{i}");

            Assert.That(composition.IsSuccess, Is.True, UniversalToolchain.Dialects.Integration.DialectCompositionExplanationFormatter.FormatDeterministic(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationProjector.Project(composition)));

            using var host = workflow.CreateHost(composition);
            var runResult = host.Run("2 + 5", "interpreter");

            signatures.Add(
                WistDialectTestInfrastructure.BuildSelectionAndDiagnosticsSignature(composition)
                + "::host::"
                + WistDialectTestInfrastructure.BuildHostSignature(host)
                + "::result::"
                + runResult);
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
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

    private static IReadOnlyList<string> BuildRegisteredTypeNames(IServiceCollection services)
    {
        return services
            .SelectMany(static service => new[]
            {
                service.ServiceType,
                service.ImplementationType,
                service.ImplementationInstance?.GetType()
            })
            .Where(static type => type != null)
            .Cast<Type>()
            .Select(static type => type.FullName ?? type.Name)
            .OrderBy(static x => x, StringComparer.Ordinal)
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

    private sealed class CountingRegistrar(string backendId) : IDialectBackendRuntimeRegistrar
    {
        public DialectBackendId BackendId { get; } = new(backendId);
        public IReadOnlyList<string> SupportedIntrinsics => [];
        public int RegisterRuntimeCallCount { get; private set; }

        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
            RegisterRuntimeCallCount++;
            services.AddSingleton(typeof(object), new object());
        }
    }
}
