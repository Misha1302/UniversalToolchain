using BasicCore.Contracts;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectBackendCompatibilityTests
{
    [Test]
    public void RuntimeKnownBackendsProvider_ReturnsBackendsFromRuntimeCatalog()
    {
        var catalog = new StaticCatalog(
            Entry("cil", []),
            Entry("interpreter"),
            Entry("foreign-backend", ["foreign"]));

        var provider = new RuntimeKnownBackendsProvider(
            catalog,
            new StubRuntimeComponentTypeLoader());

        var known = provider.GetKnownBackends();

        Assert.Multiple(() =>
        {
            Assert.That(known.Select(static x => x.CanonicalId), Is.EqualTo(new[] { "cil", "foreign-backend", "interpreter" }));
            Assert.That(known.SelectMany(static x => x.AllNames), Does.Contain("foreign-backend"));
        });
    }

    [Test]
    public void RuntimeKnownBackendsProvider_UsesCatalogOrderDeterministically()
    {
        var catalog = new StaticCatalog(Entry("interpreter"), Entry("cil"));
        var provider = new RuntimeKnownBackendsProvider(
            catalog,
            new StubRuntimeComponentTypeLoader());

        Assert.That(provider.GetKnownBackends().Select(static x => x.CanonicalId), Is.EqualTo(new[] { "cil", "interpreter" }));
    }

    [Test]
    public void WistDialectExecutionConfigurationBuilder_KnownBackendsComeFromSelectedRuntimeBackends()
    {
        var backendEntry = Entry("interpreter", ["run"]);
        var builder = new WistDialectExecutionConfigurationBuilder(
            CreateShapeBuilder(new StubRuntimeComponentTypeLoader()),
            CreateBackendConfigurationBuilder(new StubRuntimeComponentTypeLoader()));

        var configuration = builder.Build(
            new DialectBuildPlan("Demo", null, [], [new DialectBackendId("interpreter")], [], [], [], null, [], new DialectValidationResult([])),
            new SelectedRuntimePlan([], [], [backendEntry], []));

        Assert.Multiple(() =>
        {
            Assert.That(configuration.TryResolveKnownBackendId("interpreter", out var backendId), Is.True);
            Assert.That(backendId.Value, Is.EqualTo("interpreter"));
            Assert.That(configuration.TryResolveKnownBackendId("run", out var aliasId), Is.True);
            Assert.That(aliasId.Value, Is.EqualTo("interpreter"));
            Assert.That(configuration.TryResolveKnownBackendId("foreign-backend", out _), Is.False);
        });
    }

    [Test]
    public void WistDialectExecutionConfigurationBuilder_RejectsModuleSelectionWithWrongManifestKind()
    {
        var builder = CreateBuilder(new StubRuntimeComponentTypeLoader());
        var entry = new RuntimeComponentManifestEntry(
            RuntimeComponentKind.Optimizer,
            "Arithmetic",
            [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.Optimizer, "Arithmetic"),
            "AnyAssembly",
            new RuntimeComponentActivationInfo(new RuntimeTypeReference("AnyAssembly", "Test.Activation.Type")));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder.Build(
                new DialectBuildPlan("Demo", null, ["Arithmetic"], [], [], [], [], null, [], new DialectValidationResult([])),
                new SelectedRuntimePlan([entry], [], [], [])));

        Assert.That(exception!.Message, Does.Contain("FrontendModule").And.Contain("expected"));
    }

    [Test]
    public void WistDialectExecutionConfigurationBuilder_RejectsModuleSelectionWithUnsupportedActivationType()
    {
        var module = ModuleEntry("UnsupportedModule");
        var builder = CreateBuilder(new MappingRuntimeComponentTypeLoader((module, typeof(object))));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder.Build(
                new DialectBuildPlan("Demo", null, ["UnsupportedModule"], [], [], [], [], null, [], new DialectValidationResult([])),
                new SelectedRuntimePlan([module], [], [], [])));

        Assert.That(exception!.Message, Does.Contain("UnsupportedModule").And.Contain(nameof(IFrontendCoreModule)).And.Contain(nameof(IAirOptimizer)));
    }

    [Test]
    public void WistDialectExecutionConfigurationBuilder_RejectsOptimizerSelectionWithUnsupportedActivationType()
    {
        var backend = Entry("interpreter");
        var optimizer = OptimizerEntry("UnsupportedOptimization");
        var builder = CreateBuilder(new MappingRuntimeComponentTypeLoader(
            (backend, typeof(object)),
            (optimizer, typeof(object))));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder.Build(
                new DialectBuildPlan(
                    "Demo",
                    null,
                    [],
                    [new DialectBackendId("interpreter")],
                    [],
                    [],
                    [new OptimizerBuildDirective("UnsupportedOptimization", true, DialectBackendSelector.Any)],
                    null,
                    [],
                    new DialectValidationResult([])),
                new SelectedRuntimePlan([], [optimizer], [backend], [])));

        Assert.That(exception!.Message, Does.Contain("UnsupportedOptimization").And.Contain(nameof(IAirOptimizer)));
    }

    private static WistDialectExecutionConfigurationBuilder CreateBuilder(IRuntimeComponentTypeLoader typeLoader) =>
        new(
            CreateShapeBuilder(typeLoader),
            CreateBackendConfigurationBuilder(typeLoader));

    private static SelectedRuntimeExecutionShapeBuilder CreateShapeBuilder(IRuntimeComponentTypeLoader typeLoader) =>
        new(
            new SelectedRuntimeModuleClassifier(typeLoader),
            new WistRequiredInfrastructureModulesProvider());

    private static DialectBackendRuntimeConfigurationBuilder CreateBackendConfigurationBuilder(IRuntimeComponentTypeLoader typeLoader) =>
        new(
            typeLoader,
            new DialectIntrinsicPolicyResolver());

    private static RuntimeComponentManifestEntry Entry(string alias, IReadOnlyList<string>? aliases = null) =>
        new(
            RuntimeComponentKind.Backend,
            alias,
            aliases ?? [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.Backend, alias),
            "AnyAssembly",
            new RuntimeComponentActivationInfo(new RuntimeTypeReference("AnyAssembly", "Test.Activation.Type")));

    private static RuntimeComponentManifestEntry ModuleEntry(string alias) =>
        new(
            RuntimeComponentKind.FrontendModule,
            alias,
            [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.FrontendModule, alias),
            "AnyAssembly",
            new RuntimeComponentActivationInfo(new RuntimeTypeReference("AnyAssembly", "Test.Activation.Type")));

    private static RuntimeComponentManifestEntry OptimizerEntry(string alias) =>
        new(
            RuntimeComponentKind.Optimizer,
            alias,
            [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.Optimizer, alias),
            "AnyAssembly",
            new RuntimeComponentActivationInfo(new RuntimeTypeReference("AnyAssembly", "Test.Activation.Type")));

    private sealed class StaticCatalog(params RuntimeComponentManifestEntry[] backends) : IRuntimeComponentCatalog
    {
        private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _map = backends
            .SelectMany(static x => x.AllAliases.Select(alias => (alias, entry: x)))
            .ToDictionary(static x => x.alias, static x => x.entry, StringComparer.Ordinal);

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

        public bool TryResolveBackend(string alias, out RuntimeComponentManifestEntry? entry) => _map.TryGetValue(alias, out entry);


        public IReadOnlyList<RuntimeComponentManifestEntry> GetModulesInDeterministicOrder() => [];

        public IReadOnlyList<RuntimeComponentManifestEntry> GetOptimizersInDeterministicOrder() => [];

        public IReadOnlyList<RuntimeComponentManifestEntry> GetBackendsInDeterministicOrder() => _map.Values.Distinct().OrderBy(x => x.CanonicalAlias, StringComparer.Ordinal).ToList();
    }

    private sealed class StubRuntimeComponentTypeLoader : IRuntimeComponentTypeLoader
    {
        public Type LoadType(RuntimeComponentManifestEntry entry) => typeof(object);
    }

    private sealed class MappingRuntimeComponentTypeLoader(params (RuntimeComponentManifestEntry Entry, Type Type)[] mappings) : IRuntimeComponentTypeLoader
    {
        private readonly IReadOnlyDictionary<RuntimeComponentId, Type> _typesById = mappings.ToDictionary(
            static x => x.Entry.ComponentId,
            static x => x.Type);

        public Type LoadType(RuntimeComponentManifestEntry entry)
        {
            if (_typesById.TryGetValue(entry.ComponentId, out var type))
                return type;

            return typeof(object);
        }
    }
}