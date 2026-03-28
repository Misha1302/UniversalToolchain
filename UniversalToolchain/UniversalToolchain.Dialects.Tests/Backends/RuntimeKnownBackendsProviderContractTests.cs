using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests.Backends;

public class RuntimeKnownBackendsProviderContractTests
{
    [Test]
    public void KnownBackendsProvider_ShouldReturnOnlyBackendsBackedByRegistrars()
    {
        var catalog = new StaticCatalog([
            Entry(RuntimeComponentKind.Backend, "compiler", "Meta.Compiler"),
            Entry(RuntimeComponentKind.Backend, "interpreter", "Meta.Interpreter")
        ]);

        var provider = new RuntimeKnownBackendsProvider(catalog, [new StubRegistrar("interpreter")]);
        var known = provider.GetKnownBackends();

        Assert.That(known.Select(static x => x.CanonicalId), Is.EqualTo(new[] { "interpreter" }));
    }

    [Test]
    public void KnownBackendsProvider_ShouldRejectDuplicateBackendRegistrars()
    {
        var catalog = new StaticCatalog([Entry(RuntimeComponentKind.Backend, "interpreter", "Meta.Interpreter")]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new RuntimeKnownBackendsProvider(catalog, [new StubRegistrar("interpreter"), new StubRegistrar("interpreter")]))!;

        Assert.That(ex.Message, Does.Contain("Duplicate backend provider registration for backend 'interpreter'"));
    }

    [Test]
    public void KnownBackendsProvider_ShouldRejectRegistrarWithoutCatalogMetadata()
    {
        var catalog = new StaticCatalog([Entry(RuntimeComponentKind.Backend, "compiler", "Meta.Compiler")]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new RuntimeKnownBackendsProvider(catalog, [new StubRegistrar("interpreter")]))!;

        Assert.That(ex.Message, Does.Contain("no matching runtime backend metadata entry"));
    }

    [Test]
    public void KnownBackendsProvider_ShouldExposeCatalogBackedMetadataOwnershipWithoutBreakingDescriptorTruth()
    {
        var catalog = new StaticCatalog([Entry(RuntimeComponentKind.Backend, "interpreter", "Meta.Interpreter", "vm")]);

        var provider = new RuntimeKnownBackendsProvider(catalog, [new StubRegistrar("interpreter")]);
        var descriptor = provider.GetKnownBackends().Single();

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.MetadataOwnerType, Is.Not.Null);
            Assert.That(descriptor.MetadataOwnerType.Assembly, Is.EqualTo(typeof(RuntimeComponentManifestEntry).Assembly));
            Assert.That(descriptor.CanonicalId, Is.EqualTo("interpreter"));
            Assert.That(descriptor.Aliases, Is.EqualTo(new[] { "vm" }));
        });
    }

    [Test]
    public void KnownBackendsProvider_ShouldSortBackendsDeterministically()
    {
        var catalog = new StaticCatalog([
            Entry(RuntimeComponentKind.Backend, "interpreter", "Meta.Interpreter"),
            Entry(RuntimeComponentKind.Backend, "compiler", "Meta.Compiler")
        ]);

        var provider = new RuntimeKnownBackendsProvider(catalog, [new StubRegistrar("interpreter"), new StubRegistrar("compiler")]);

        Assert.That(provider.GetKnownBackends().Select(static x => x.CanonicalId), Is.EqualTo(new[] { "compiler", "interpreter" }));
    }

    [Test]
    public void KnownBackendsProvider_ShouldPreserveAliasesDeterministically()
    {
        var catalog = new StaticCatalog([Entry(RuntimeComponentKind.Backend, "interpreter", "Meta.Interpreter", "z", "a")]);

        var provider = new RuntimeKnownBackendsProvider(catalog, [new StubRegistrar("interpreter")]);
        var aliases = provider.GetKnownBackends().Single().Aliases;

        Assert.That(aliases, Is.EqualTo(new[] { "a", "z" }));
    }

    private static RuntimeComponentManifestEntry Entry(RuntimeComponentKind kind, string canonical, string type, params string[] aliases)
        => new(kind, canonical, aliases, new RuntimeTypeReference("Assembly", type));

    private sealed class StubRegistrar(string backend) : IDialectBackendRuntimeRegistrar
    {
        public DialectBackendId BackendId { get; } = new(backend);
        public IReadOnlyList<string> SupportedIntrinsics => [];

        public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
        {
        }
    }

    private sealed class StaticCatalog(IEnumerable<RuntimeComponentManifestEntry> entries) : IRuntimeComponentCatalog
    {
        private readonly IReadOnlyDictionary<string, RuntimeComponentManifestEntry> _entries = entries
            .Where(static x => x.Kind == RuntimeComponentKind.Backend)
            .SelectMany(static x => x.AllAliases.Select(a => (a, x)))
            .ToDictionary(static x => x.a, static x => x.x, StringComparer.Ordinal);

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

        public bool TryResolveBackend(string alias, out RuntimeComponentManifestEntry? entry) => _entries.TryGetValue(alias, out entry);
        public IReadOnlyList<RuntimeComponentManifestEntry> GetModulesInDeterministicOrder() => [];
        public IReadOnlyList<RuntimeComponentManifestEntry> GetOptimizersInDeterministicOrder() => [];
        public IReadOnlyList<RuntimeComponentManifestEntry> GetBackendsInDeterministicOrder() => _entries.Values.Distinct().OrderBy(static x => x.CanonicalAlias).ToList();
    }
}