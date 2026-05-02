namespace UniversalToolchain.Dialects.Tests.Backends;

public class RuntimeKnownBackendsProviderContractTests
{
    [Test]
    public void KnownBackendsProvider_ShouldReturnCatalogBackends()
    {
        var catalog = new StaticCatalog([
            Entry(RuntimeComponentKind.Backend, "compiler", "Meta.Compiler"),
            Entry(RuntimeComponentKind.Backend, "interpreter", "Meta.Interpreter")
        ]);

        var provider = new RuntimeKnownBackendsProvider(catalog, new StubTypeLoader());
        var known = provider.GetKnownBackends();

        Assert.That(known.Select(static x => x.CanonicalId), Is.EqualTo(new[] { "compiler", "interpreter" }));
    }

    [Test]
    public void KnownBackendsProvider_ShouldResolveRealMetadataOwnerType_FromTypeLoader()
    {
        var catalog = new StaticCatalog([Entry(RuntimeComponentKind.Backend, "interpreter", "Meta.Interpreter", "vm")]);

        var provider = new RuntimeKnownBackendsProvider(catalog, new StubTypeLoader());
        var descriptor = provider.GetKnownBackends().Single();

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.MetadataOwnerType, Is.EqualTo(typeof(FakeBackendMetadata)));
            Assert.That(descriptor.CanonicalId, Is.EqualTo("interpreter"));
            Assert.That(descriptor.Aliases, Is.EqualTo(new[] { "vm" }));
        });
    }

    [Test]
    public void KnownBackendsProvider_ShouldBuildMetadataLazily()
    {
        var catalog = new StaticCatalog([Entry(RuntimeComponentKind.Backend, "interpreter", "Meta.Interpreter")]);
        var typeLoader = new StubTypeLoader();

        var provider = new RuntimeKnownBackendsProvider(catalog, typeLoader);

        Assert.That(typeLoader.Calls, Is.EqualTo(0));

        provider.GetKnownBackends();
        provider.GetKnownBackends();

        Assert.That(typeLoader.Calls, Is.EqualTo(1));
    }

    [Test]
    public void KnownBackendsProvider_ShouldSortBackendsDeterministically()
    {
        var catalog = new StaticCatalog([
            Entry(RuntimeComponentKind.Backend, "interpreter", "Meta.Interpreter"),
            Entry(RuntimeComponentKind.Backend, "compiler", "Meta.Compiler")
        ]);

        var provider = new RuntimeKnownBackendsProvider(
            catalog,
            new StubTypeLoader());

        Assert.That(provider.GetKnownBackends().Select(static x => x.CanonicalId), Is.EqualTo(new[] { "compiler", "interpreter" }));
    }

    [Test]
    public void KnownBackendsProvider_ShouldPreserveAliasesDeterministically()
    {
        var catalog = new StaticCatalog([Entry(RuntimeComponentKind.Backend, "interpreter", "Meta.Interpreter", "z", "a")]);

        var provider = new RuntimeKnownBackendsProvider(catalog, new StubTypeLoader());
        var aliases = provider.GetKnownBackends().Single().Aliases;

        Assert.That(aliases, Is.EqualTo(new[] { "a", "z" }));
    }

    private static RuntimeComponentManifestEntry Entry(RuntimeComponentKind kind, string canonical, string _, params string[] aliases)
        => new(kind, canonical, aliases, RuntimeComponentIdFactory.Create(kind, canonical), "Assembly");

    private sealed class StubTypeLoader : IRuntimeComponentTypeLoader
    {
        public int Calls { get; private set; }

        public Type LoadType(RuntimeComponentManifestEntry entry)
        {
            Calls++;
            return typeof(FakeBackendMetadata);
        }
    }

    private sealed class FakeBackendMetadata
    {
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