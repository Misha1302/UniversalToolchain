using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectBackendCompatibilityTests
{
    [Test]
    public void WistKnownBackendsProvider_ReturnsOnlyBackendsSupportedByWistProviders()
    {
        var catalog = new StaticCatalog(
            Entry("cil", ["compiler"]),
            Entry("interpreter"),
            Entry("foreign-backend", ["foreign"]));

        var provider = new RuntimeKnownBackendsProvider(
            catalog,
            [
                new StubBackendRegistrar(new DialectBackendId("cil")),
                new StubBackendRegistrar(new DialectBackendId("interpreter"))
            ]);

        var known = provider.GetKnownBackends();

        Assert.Multiple(() =>
        {
            Assert.That(known.Select(static x => x.CanonicalId), Is.EqualTo(new[] { "cil", "interpreter" }));
            Assert.That(known.SelectMany(static x => x.AllNames), Does.Not.Contain("foreign-backend"));
        });
    }

    [Test]
    public void WistKnownBackendsProvider_FailsWhenProviderBackendIsMissingFromCatalog()
    {
        var catalog = new StaticCatalog(Entry("cil"));
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new RuntimeKnownBackendsProvider(catalog, [new StubBackendRegistrar(new DialectBackendId("interpreter"))]));

        Assert.That(exception!.Message, Does.Contain("interpreter"));
    }

    [Test]
    public void WistKnownBackendsProvider_FailsOnDuplicateBackendProvidersDeterministically()
    {
        var catalog = new StaticCatalog(Entry("cil"));
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new RuntimeKnownBackendsProvider(
                catalog,
                [
                    new StubBackendRegistrar(new DialectBackendId("cil")),
                    new StubBackendRegistrar(new DialectBackendId("cil"))
                ]));

        Assert.That(exception!.Message, Does.Contain("Duplicate").And.Contain("cil"));
    }

    [Test]
    public void WistDialectExecutionConfigurationBuilder_KnownBackendsComeFromWistKnownBackendsProvider()
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

    private static RuntimeComponentManifestEntry Entry(string alias, IReadOnlyList<string>? aliases = null)
    {
        return new RuntimeComponentManifestEntry(
            RuntimeComponentKind.Backend,
            alias,
            aliases ?? [],
            new RuntimeTypeReference("AnyAssembly", $"Any.Namespace.{alias}"));
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

    private sealed class StubBackendRegistrar(DialectBackendId backendId) : IDialectBackendRuntimeRegistrar
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
