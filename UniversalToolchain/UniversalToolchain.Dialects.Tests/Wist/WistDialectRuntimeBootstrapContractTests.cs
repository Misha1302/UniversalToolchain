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
    public void AddWistDialectCoreServices_ShouldRegisterIntrinsicBootstrapOrchestrationServices()
    {
        var services = new ServiceCollection();
        services.AddWistDialectCoreServices();

        Assert.Multiple(() =>
        {
            Assert.That(services.Any(static x => x.ServiceType == typeof(IntrinsicSemanticBootstrapPlanBuilder)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IntrinsicSemanticBootstrapPreProviderValidator)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IntrinsicSemanticBootstrapRuntimeValidator)), Is.True);
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
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeAssemblyTypeLoader)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeComponentTypeLoader)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeBackendRegistrarResolver)), Is.True);
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
    public void WistDialectServicesRegistrar_ShouldNotRegisterCompatibilityBackendRegistrars()
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
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var composition = workflow.ComposeText("dialect ComposeOnly\nuse Arithmetic,Numbers\nbackend interpreter", "compose-only");

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));
            Assert.That(composition.RuntimeSelection, Is.InstanceOf<SelectedRuntimePlan>());
        });

        using var host = workflow.CreateHost(composition);

        Assert.That(host.Configuration.EnabledBackends.Select(static x => x.CanonicalId), Is.EqualTo(new[] { "interpreter" }));
    }

    [Test]
    public void AddWistDialectServices_CanonicalPath_ShouldCreateHostForManifestSelectedBackend()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect MissingBackends\nuse Arithmetic\nbackend interpreter", "missing-backends");

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        using var host = workflow.CreateHost(composition);
        Assert.That(host.Configuration.EnabledBackends.Select(static x => x.CanonicalId), Is.EqualTo(new[] { "interpreter" }));
    }

    [Test]
    public void AddWistDialectServices_CanonicalPath_ShouldCreateHostForShippedBackends()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect ShippedBackends\nuse Arithmetic\nbackend interpreter,compiler", "shipped-backends");

        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        using var host = workflow.CreateHost(composition);
        Assert.That(host.Configuration.EnabledBackends.Select(static x => x.CanonicalId), Is.EqualTo(new[] { "cil", "interpreter" }));
    }

    [Test]
    public void ExplicitBackendRegistration_ShouldNotLimitManifestDrivenBackendActivation()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var interpreterOnly = workflow.ComposeText("dialect InterpreterOnly\nuse Arithmetic\nbackend interpreter", "interpreter-only");
        var compilerRequested = workflow.ComposeText("dialect NeedsCompiler\nuse Arithmetic\nbackend compiler", "needs-compiler");

        Assert.That(interpreterOnly.IsSuccess, Is.True, FormatComposition(interpreterOnly));
        using var interpreterHost = workflow.CreateHost(interpreterOnly);

        Assert.That(compilerRequested.IsSuccess, Is.True, FormatComposition(compilerRequested));
        using var compilerHost = workflow.CreateHost(compilerRequested);

        Assert.Multiple(() =>
        {
            Assert.That(interpreterHost.Configuration.EnabledBackends.Select(static x => x.CanonicalId), Is.EqualTo(new[] { "interpreter" }));
            Assert.That(compilerHost.Configuration.EnabledBackends.Select(static x => x.CanonicalId), Is.EqualTo(new[] { "cil" }));
        });
    }

    [Test]
    public void CanonicalBootstrap_ShouldRemainStableAcrossRepeatedServiceProviderBuilds()
    {
        var signatures = new List<string>();
        for (var i = 0; i < 30; i++)
        {
            var services = new ServiceCollection();
            services.AddWistDialectServices();

            using var provider = services.BuildServiceProvider();
            var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
            var composition = workflow.ComposeText("dialect Stable\nuse Arithmetic,Numbers\nbackend interpreter,compiler", $"stable-{i}");
            Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

            using var host = workflow.CreateHost(composition);
            signatures.Add(WistDialectTestInfrastructure.BuildHostSignature(host));
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
    }

    [Test]
    public void WistDialectServiceProviderFactory_ShouldRegisterFrontendDefaultsWithoutBackendDefaults()
    {
        var backendEntry = BackendEntry("interpreter", typeof(NoopRegistrar));
        var factory = CreateFactory([new NoopRegistrar("interpreter")]);
        var config = new WistDialectExecutionConfiguration(
            "Demo",
            [],
            [],
            [],
            [new DialectBackendRuntimeConfiguration(backendEntry, new RuntimeBackendDescriptor(new DialectBackendId("interpreter"), typeof(NoopRegistrar), ["vm"]), [], [], [], false)],
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
    public void WistDialectServiceProviderFactory_ShouldResolveAndActivateOnlySelectedBackends()
    {
        var selectedEntry = BackendEntry("selected", typeof(CountingRegistrar));
        var resolver = new RecordingBackendRegistrarResolver([
            ("selected", new CountingRegistrar("selected")),
            ("unselected", new CountingRegistrar("unselected"))
        ]);
        var factory = new WistDialectServiceProviderFactory(
            resolver,
            new IntrinsicSemanticBootstrapPlanBuilder(),
            new IntrinsicSemanticBootstrapPreProviderValidator(),
            new IntrinsicSemanticBootstrapRuntimeValidator());
        var config = new WistDialectExecutionConfiguration(
            "Demo",
            [],
            [],
            [],
            [new DialectBackendRuntimeConfiguration(selectedEntry, new RuntimeBackendDescriptor(new DialectBackendId("selected"), typeof(CountingRegistrar)), [], [], [], false)],
            [
                new RuntimeBackendDescriptor(new DialectBackendId("selected"), typeof(CountingRegistrar)),
                new RuntimeBackendDescriptor(new DialectBackendId("unselected"), typeof(CountingRegistrar))
            ]);

        using var provider = factory.Create(config) as IDisposable;

        Assert.Multiple(() =>
        {
            Assert.That(resolver.ResolvedBackendAliases, Is.EqualTo(new[] { "selected" }));
            Assert.That(((CountingRegistrar)resolver.RegistrarsByAlias["selected"]).RegisterRuntimeCallCount, Is.EqualTo(1));
            Assert.That(((CountingRegistrar)resolver.RegistrarsByAlias["unselected"]).RegisterRuntimeCallCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void CreateHost_ShouldFailClearly_IfBackendConfigurationDoesNotCarryManifestEntry()
    {
        var factory = CreateFactory([]);
        var config = new WistDialectExecutionConfiguration(
            "Demo",
            [],
            [],
            [],
            [new DialectBackendRuntimeConfiguration(new RuntimeBackendDescriptor(new DialectBackendId("interpreter"), typeof(object), []), [], [], [], false)],
            [new RuntimeBackendDescriptor(new DialectBackendId("interpreter"), typeof(object), [])]);

        var ex = Assert.Throws<InvalidOperationException>(() => factory.Create(config));

        Assert.That(ex!.Message, Does.Contain("does not include the selected backend manifest entry"));
    }

    [Test]
    public void ComposeCreateRun_CanonicalPath_RepeatedCycles_ShouldKeepDeterministicCompositeSignature()
    {
        using var provider = WistDialectTestInfrastructure.CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var signatures = new List<string>();
        for (var i = 0; i < 30; i++)
        {
            var composition = workflow.ComposeText(
                "dialect Stable\nuse Arithmetic,Numbers,Whitespaces\nenable LocalVariablesOptimization\nbackend interpreter,compiler",
                $"stable-{i}");

            Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

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


    private static WistDialectServiceProviderFactory CreateFactory(IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars)
    {
        return new WistDialectServiceProviderFactory(
            new StaticBackendRegistrarResolver(backendRegistrars),
            new IntrinsicSemanticBootstrapPlanBuilder(),
            new IntrinsicSemanticBootstrapPreProviderValidator(),
            new IntrinsicSemanticBootstrapRuntimeValidator());
    }

    private static RuntimeComponentManifestEntry BackendEntry(string alias, Type registrarType)
        => new(
            RuntimeComponentKind.Backend,
            alias,
            [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.Backend, alias),
            registrarType.Assembly.GetName().Name!,
            new RuntimeComponentActivationInfo(typeof(object).FullName!, registrarType.FullName));

    private static string FormatComposition(DialectFrameworkCompositionResult composition)
    {
        return DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition));
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

    private sealed class StaticBackendRegistrarResolver(IEnumerable<IDialectBackendRuntimeRegistrar> backendRegistrars) : IRuntimeBackendRegistrarResolver
    {
        private readonly IReadOnlyDictionary<DialectBackendId, IDialectBackendRuntimeRegistrar> _registrarsById = backendRegistrars.ToDictionary(
            static x => x.BackendId,
            static x => x);

        public IDialectBackendRuntimeRegistrar Resolve(RuntimeComponentManifestEntry backendEntry)
        {
            if (_registrarsById.TryGetValue(new DialectBackendId(backendEntry.CanonicalAlias), out var registrar))
            {
                return registrar;
            }

            throw new InvalidOperationException($"No test backend runtime registrar is registered for backend '{backendEntry.CanonicalAlias}'.");
        }
    }

    private sealed class RecordingBackendRegistrarResolver(IEnumerable<(string Alias, IDialectBackendRuntimeRegistrar Registrar)> registrars) : IRuntimeBackendRegistrarResolver
    {
        private readonly List<string> _resolvedBackendAliases = [];

        public IReadOnlyDictionary<string, IDialectBackendRuntimeRegistrar> RegistrarsByAlias { get; } = registrars.ToDictionary(
            static x => x.Alias,
            static x => x.Registrar,
            StringComparer.Ordinal);

        public IReadOnlyList<string> ResolvedBackendAliases => _resolvedBackendAliases;

        public IDialectBackendRuntimeRegistrar Resolve(RuntimeComponentManifestEntry backendEntry)
        {
            _resolvedBackendAliases.Add(backendEntry.CanonicalAlias);
            return RegistrarsByAlias[backendEntry.CanonicalAlias];
        }
    }
}
