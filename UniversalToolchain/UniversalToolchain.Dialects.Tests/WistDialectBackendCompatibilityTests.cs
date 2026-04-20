using BasicCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectBackendCompatibilityTests
{
    [Test]
    public void RuntimeKnownBackendsProvider_ReturnsOnlyBackendsSupportedByRuntimeRegistrars()
    {
        var catalog = new StaticCatalog(
            Entry("cil", ["compiler"]),
            Entry("interpreter"),
            Entry("foreign-backend", ["foreign"]));

        var provider = new RuntimeKnownBackendsProvider(
            catalog,
            [
                new StubBackendProvider(new DialectBackendId("cil")),
                new StubBackendProvider(new DialectBackendId("interpreter"))
            ],
            new StubRuntimeComponentTypeLoader());

        var known = provider.GetKnownBackends();

        Assert.Multiple(() =>
        {
            Assert.That(known.Select(static x => x.CanonicalId), Is.EqualTo(new[] { "cil", "interpreter" }));
            Assert.That(known.SelectMany(static x => x.AllNames), Does.Not.Contain("foreign-backend"));
        });
    }

    [Test]
    public void RuntimeKnownBackendsProvider_FailsWhenProviderBackendIsMissingFromCatalog()
    {
        var catalog = new StaticCatalog(Entry("cil"));
        var provider = new RuntimeKnownBackendsProvider(
            catalog,
            [new StubBackendProvider(new DialectBackendId("interpreter"))],
            new StubRuntimeComponentTypeLoader());

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetKnownBackends());

        Assert.That(exception!.Message, Does.Contain("interpreter"));
    }

    [Test]
    public void RuntimeKnownBackendsProvider_FailsOnDuplicateBackendProvidersDeterministically()
    {
        var catalog = new StaticCatalog(Entry("cil"));
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new RuntimeKnownBackendsProvider(
                catalog,
                [
                    new StubBackendProvider(new DialectBackendId("cil")),
                    new StubBackendProvider(new DialectBackendId("cil"))
                ],
                new StubRuntimeComponentTypeLoader()));

        Assert.That(exception!.Message, Does.Contain("Duplicate").And.Contain("cil"));
    }

    [Test]
    public void WistDialectExecutionConfigurationBuilder_KnownBackendsComeFromRuntimeKnownBackendsProvider()
    {
        var knownBackendsProvider = new RecordingKnownBackendsProvider(
            [new RuntimeBackendDescriptor(new DialectBackendId("wist-only"), ["wo"])]);
        var builder = new WistDialectExecutionConfigurationBuilder(
            new StubRuntimeComponentTypeLoader(),
            new DialectIntrinsicPolicyResolver(),
            knownBackendsProvider);

        var configuration = builder.Build(
            new DialectBuildPlan("Demo", null, [], [], [], [], [], null, [], new DialectValidationResult([])),
            new SelectedRuntimePlan([], [], [], []));

        Assert.Multiple(() =>
        {
            Assert.That(knownBackendsProvider.Calls, Is.EqualTo(1));
            Assert.That(configuration.TryResolveKnownBackendId("wist-only", out var backendId), Is.True);
            Assert.That(backendId.Value, Is.EqualTo("wist-only"));
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
            "AnyAssembly");

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

        Assert.That(exception!.Message, Does.Contain("UnsupportedModule").And.Contain(nameof(IFrontendCoreModule)).And.Contain(nameof(IIRProcessingModule)));
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

        Assert.That(exception!.Message, Does.Contain("UnsupportedOptimization").And.Contain(nameof(IIRProcessingModule)));
    }

    private static WistDialectExecutionConfigurationBuilder CreateBuilder(IRuntimeComponentTypeLoader typeLoader)
    {
        return new WistDialectExecutionConfigurationBuilder(
            typeLoader,
            new DialectIntrinsicPolicyResolver(),
            new RecordingKnownBackendsProvider([]));
    }

    private static RuntimeComponentManifestEntry Entry(string alias, IReadOnlyList<string>? aliases = null) =>
        new(
            RuntimeComponentKind.Backend,
            alias,
            aliases ?? [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.Backend, alias),
            "AnyAssembly");

    private static RuntimeComponentManifestEntry ModuleEntry(string alias)
    {
        return new RuntimeComponentManifestEntry(
            RuntimeComponentKind.FrontendModule,
            alias,
            [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.FrontendModule, alias),
            "AnyAssembly");
    }

    private static RuntimeComponentManifestEntry OptimizerEntry(string alias)
    {
        return new RuntimeComponentManifestEntry(
            RuntimeComponentKind.Optimizer,
            alias,
            [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.Optimizer, alias),
            "AnyAssembly");
    }

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

    private sealed class StubBackendProvider(DialectBackendId backendId) : IDialectBackendRuntimeRegistrar
    {
        public DialectBackendId BackendId { get; } = backendId;

        public IReadOnlyList<string> SupportedIntrinsics => [];

        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
        }
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
            {
                return type;
            }

            return typeof(object);
        }
    }

    private sealed class RecordingKnownBackendsProvider(IReadOnlyList<RuntimeBackendDescriptor> knownBackends) : IRuntimeKnownBackendsProvider
    {
        public int Calls { get; private set; }

        public IReadOnlyList<RuntimeBackendDescriptor> GetKnownBackends()
        {
            Calls++;
            return knownBackends;
        }
    }
}
