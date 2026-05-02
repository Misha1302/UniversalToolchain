using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public sealed class ThirdBackendRuntimeComponentContractTests
{
    private const string ThirdBackendId = "test-third";
    private const string ThirdBackendAlias = "third";
    private const string ThirdBackendSecondAlias = "plugin-third";
    private const string ThirdBackendAssembly = "ThirdBackendAssembly";

    [Test]
    public void Resolve_BackendAlias_SelectsThirdBackendManifestEntry()
    {
        var backendEntry = ThirdBackendEntry();
        var resolver = new SelectedRuntimePlanResolver(new StubRuntimeComponentCatalog([backendEntry]));
        var buildPlan = BuildPlan([new DialectBackendId(ThirdBackendAlias)]);

        var selectedRuntimePlan = resolver.Resolve(buildPlan);

        Assert.Multiple(() =>
        {
            Assert.That(selectedRuntimePlan.IsResolved, Is.True);
            Assert.That(selectedRuntimePlan.EnabledBackends, Has.Count.EqualTo(1));
            Assert.That(selectedRuntimePlan.EnabledBackends[0].CanonicalAlias, Is.EqualTo(ThirdBackendId));
        });
    }

    [Test]
    public void Resolve_UnknownBackend_ReportsUnregisteredBackendDiagnostic()
    {
        var resolver = new SelectedRuntimePlanResolver(new StubRuntimeComponentCatalog([]));
        var buildPlan = BuildPlan([new DialectBackendId("missing-backend")]);

        var selectedRuntimePlan = resolver.Resolve(buildPlan);

        Assert.Multiple(() =>
        {
            Assert.That(selectedRuntimePlan.IsResolved, Is.False);
            Assert.That(
                selectedRuntimePlan.Diagnostics,
                Has.Some.Matches<DialectDiagnostic>(x =>
                    x.Code == "R002" &&
                    x.Message.Contains("missing-backend", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void ExecutionConfiguration_KnownBackendMap_UsesCanonicalIdAndAliasesFromManifest()
    {
        var configuration = ConfigurationFor(ThirdBackendEntry());

        Assert.Multiple(() =>
        {
            AssertBackendName(configuration, ThirdBackendId);
            AssertBackendName(configuration, ThirdBackendAlias);
            AssertBackendName(configuration, ThirdBackendSecondAlias);
        });
    }

    [Test]
    public void ServiceProviderFactory_ActivatesThirdBackendRegistrarFromSelectedManifestEntry()
    {
        var backendEntry = ThirdBackendEntry();
        var registrarResolver = new StubRuntimeBackendRegistrarResolver(new ThirdBackendRegistrar());
        var factory = new WistDialectServiceProviderFactory(
            registrarResolver,
            new IntrinsicSemanticBootstrapPlanBuilder(),
            new IntrinsicSemanticBootstrapPreProviderValidator(),
            new IntrinsicSemanticBootstrapRuntimeValidator());

        var serviceProvider = factory.Create(ConfigurationFor(backendEntry));
        try
        {
            var marker = serviceProvider.GetRequiredService<ThirdBackendActivationMarker>();

            Assert.Multiple(() =>
            {
                Assert.That(marker.BackendId, Is.EqualTo(ThirdBackendId));
                Assert.That(registrarResolver.ResolvedAliases, Is.EqualTo(new[] { ThirdBackendId }));
            });
        }
        finally
        {
            if (serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Test]
    public void BackendRuntimeRegistrarBase_IsPublicForExternalBackendModules()
    {
        Assert.That(typeof(DialectBackendRuntimeRegistrarBase<>).IsPublic, Is.True);
    }

    private static void AssertBackendName(WistDialectExecutionConfiguration configuration, string nameOrAlias)
    {
        var resolved = configuration.TryResolveKnownBackendId(nameOrAlias, out var backendId);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.True, $"Backend name or alias '{nameOrAlias}' should be known.");
            Assert.That(backendId.Value, Is.EqualTo(ThirdBackendId));
        });
    }

    private static WistDialectExecutionConfiguration ConfigurationFor(RuntimeComponentManifestEntry backendEntry)
    {
        var descriptor = new RuntimeBackendDescriptor(
            new DialectBackendId(backendEntry.CanonicalAlias),
            typeof(ThirdBackendDeclaration),
            backendEntry.Aliases);
        var backendConfiguration = new DialectBackendRuntimeConfiguration(
            backendEntry,
            descriptor,
            [],
            [],
            [],
            false);

        return new WistDialectExecutionConfiguration(
            "third-backend-test-dialect",
            [],
            [],
            [],
            [backendConfiguration],
            [descriptor],
            []);
    }

    private static DialectBuildPlan BuildPlan(IEnumerable<DialectBackendId> enabledBackends)
    {
        return new DialectBuildPlan(
            "third-backend-test-dialect",
            null,
            [],
            enabledBackends,
            [],
            [],
            [],
            null,
            [],
            new DialectValidationResult());
    }

    private static RuntimeComponentManifestEntry ThirdBackendEntry()
    {
        return new RuntimeComponentManifestEntry(
            RuntimeComponentKind.Backend,
            ThirdBackendId,
            [ThirdBackendAlias, ThirdBackendSecondAlias],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.Backend, ThirdBackendId),
            ThirdBackendAssembly,
            new RuntimeComponentActivationInfo(
                new RuntimeTypeReference(ThirdBackendAssembly, typeof(ThirdBackendDeclaration).FullName!),
                new RuntimeTypeReference(ThirdBackendAssembly, typeof(ThirdBackendRegistrar).FullName!)));
    }

    // Intentionally no DialectRuntimeExport/DialectRuntimeAlias/DialectBackendRegistrarType attributes here.
    // The test creates RuntimeComponentManifestEntry manually. Exporting this fake backend from
    // the test assembly would make the normal manifest emitter publish it into the shared runtime
    // catalog and would contaminate unrelated canonical Wist runtime tests.
    private sealed class ThirdBackendDeclaration : DialectBackendDeclaration
    {
        public override DialectBackendId BackendId => new(ThirdBackendId);
    }

    private sealed class ThirdBackendRegistrar : IDialectBackendRuntimeRegistrar
    {
        public DialectBackendId BackendId => new(ThirdBackendId);

        public IReadOnlyList<string> SupportedIntrinsics => [];

        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
            services.AddSingleton(new ThirdBackendActivationMarker(configuration.BackendDescriptor.CanonicalId));
        }
    }

    private sealed record ThirdBackendActivationMarker(string BackendId);

    private sealed class StubRuntimeBackendRegistrarResolver(IDialectBackendRuntimeRegistrar registrar) : IRuntimeBackendRegistrarResolver
    {
        private readonly List<string> _resolvedAliases = [];

        public IReadOnlyList<string> ResolvedAliases => _resolvedAliases;

        public IDialectBackendRuntimeRegistrar Resolve(RuntimeComponentManifestEntry backendEntry)
        {
            _resolvedAliases.Add(backendEntry.CanonicalAlias);
            return registrar;
        }
    }

    private sealed class StubRuntimeComponentCatalog : IRuntimeComponentCatalog
    {
        private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _backendsByAlias;
        private readonly IReadOnlyList<RuntimeComponentManifestEntry> _backendsInOrder;

        public StubRuntimeComponentCatalog(IEnumerable<RuntimeComponentManifestEntry> backends)
        {
            _backendsInOrder = backends
                .OrderBy(static x => x.CanonicalAlias, StringComparer.Ordinal)
                .ThenBy(static x => x.ComponentId.Value, StringComparer.Ordinal)
                .ToList();
            _backendsByAlias = _backendsInOrder
                .SelectMany(static x => x.AllAliases.Select(alias => new KeyValuePair<string, RuntimeComponentManifestEntry>(alias, x)))
                .ToDictionary(static x => x.Key, static x => x.Value, StringComparer.Ordinal);
        }

        public bool TryResolveModule(string alias, out RuntimeComponentManifestEntry? entry)
        {
            entry = null;
            return false;
        }

        public bool TryResolveOptimizer(string alias, out RuntimeComponentManifestEntry? entry)
        {
            entry = null;
            return false;
        }

        public bool TryResolveBackend(string alias, out RuntimeComponentManifestEntry? entry) => _backendsByAlias.TryGetValue(alias, out entry);

        public IReadOnlyList<RuntimeComponentManifestEntry> GetModulesInDeterministicOrder() => [];

        public IReadOnlyList<RuntimeComponentManifestEntry> GetOptimizersInDeterministicOrder() => [];

        public IReadOnlyList<RuntimeComponentManifestEntry> GetBackendsInDeterministicOrder() => _backendsInOrder;
    }
}
