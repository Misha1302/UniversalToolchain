using System.Collections.Concurrent;
using System.Reflection;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class DefaultRuntimeComponentResolverTests
{
    private const string TestAssemblyName = "ResolverTestsAssembly";
    private const string ResolverExportForCachingAlias = "resolver.cache.sample";
    private const string ResolverExportForAliasesAlias = "resolver.aliases.sample";
    private const string ResolverExportForDifferentEntriesAAlias = "resolver.entries.a";
    private const string ResolverExportForDifferentEntriesBAlias = "resolver.entries.b";
    private const string ResolverExportForFallbackAlias = "resolver.fallback.sample";
    private const string ResolverExportDuplicateAlias = "resolver.duplicate.sample";
    private const string ResolverExactExportAlias = "resolver.exact.sample";
    private const string ResolverExactNoExportAlias = "resolver.exact.no-export";
    private const string ManifestAuthoritativeAlias = "resolver.manifest.authoritative";

    [Test]
    public void Resolve_SameEntryTwice_ReturnsEquivalentDescriptor()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateTestAssembly(typeof(ResolverExportForCaching))
        });
        var resolver = CreateResolver(strategy);
        var entry = Entry(RuntimeComponentKind.FrontendModule, ResolverExportForCachingAlias, TestAssemblyName);

        var first = resolver.Resolve(entry);
        var second = resolver.Resolve(entry);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(second.ActivationType, Is.SameAs(first.ActivationType));
        });
    }

    [Test]
    public void Resolve_SameEntryTwice_UsesCache()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateTestAssembly(typeof(ResolverExportForCaching))
        });
        var resolver = CreateResolver(strategy);
        var entry = Entry(RuntimeComponentKind.FrontendModule, ResolverExportForCachingAlias, TestAssemblyName);

        var first = resolver.Resolve(entry);
        var second = resolver.Resolve(entry);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(strategy.GetCalls(TestAssemblyName), Is.EqualTo(1));
        });
    }

    [Test]
    public void Resolve_MissingComponent_ThrowsDeterministicError()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateTestAssembly(typeof(ResolverExportForCaching))
        });
        var resolver = CreateResolver(strategy);
        var entry = Entry(RuntimeComponentKind.FrontendModule, "resolver.missing.component", TestAssemblyName);

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(entry));

        Assert.That(
            ex!.Message,
            Is.EqualTo($"Runtime component '{entry.ComponentId}' was not found in assembly '{entry.AssemblySimpleName}'."));
    }

    [Test]
    public void Resolve_ReturnsManifestAliases()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateTestAssembly(typeof(ResolverExportForAliases))
        });
        var resolver = CreateResolver(strategy);
        var entry = new RuntimeComponentManifestEntry(
            RuntimeComponentKind.FrontendModule,
            ResolverExportForAliasesAlias,
            ["manifest.alias", "beta"],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.FrontendModule, ResolverExportForAliasesAlias),
            TestAssemblyName);

        var descriptor = resolver.Resolve(entry);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Id, Is.EqualTo(entry.ComponentId));
            Assert.That(descriptor.Kind, Is.EqualTo(entry.Kind));
            Assert.That(descriptor.CanonicalAlias, Is.EqualTo(entry.CanonicalAlias));
            Assert.That(descriptor.Aliases, Is.EqualTo(entry.Aliases));
            Assert.That(descriptor.ActivationType, Is.EqualTo(typeof(ResolverExportForAliases)));
        });
    }

    [Test]
    public void Resolve_WhenAssemblyContainsDuplicateRuntimeComponentIds_ThrowsInvalidOperationException()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateTestAssembly(typeof(ResolverExportDuplicateA), typeof(ResolverExportDuplicateB))
        });
        var resolver = CreateResolver(strategy);
        var entry = Entry(RuntimeComponentKind.FrontendModule, ResolverExportDuplicateAlias, TestAssemblyName);

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve(entry));
    }

    [Test]
    public async Task Resolve_ParallelCalls_ForSameEntry_AreConsistent()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateTestAssembly(typeof(ResolverExportForCaching))
        });
        var resolver = CreateResolver(strategy);
        var entry = Entry(RuntimeComponentKind.FrontendModule, ResolverExportForCachingAlias, TestAssemblyName);

        var tasks = Enumerable.Range(0, 64).Select(_ => Task.Run(() => resolver.Resolve(entry)));
        var resolved = await Task.WhenAll(tasks);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Distinct().Count(), Is.EqualTo(1));
            Assert.That(strategy.GetCalls(TestAssemblyName), Is.EqualTo(1));
        });
    }

    [Test]
    public void LoadType_UsesResolverActivationType()
    {
        var expectedType = typeof(ResolverExportForDifferentEntriesA);
        var resolver = new StubRuntimeComponentResolver(new RuntimeComponentDescriptor(
            Entry(RuntimeComponentKind.FrontendModule, "resolver.loader.type", TestAssemblyName).ComponentId,
            RuntimeComponentKind.FrontendModule,
            "resolver.loader.type",
            [],
            expectedType));

        var loader = new DefaultRuntimeComponentTypeLoader(resolver);
        var loadedType = loader.LoadType(Entry(RuntimeComponentKind.FrontendModule, "resolver.loader.type", TestAssemblyName));

        Assert.That(loadedType, Is.SameAs(expectedType));
    }

    [Test]
    public void Resolve_DifferentEntriesFromSameAssembly_LoadAssemblyCalledOnce()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateTestAssembly(typeof(ResolverExportForDifferentEntriesA), typeof(ResolverExportForDifferentEntriesB))
        });
        var resolver = CreateResolver(strategy);
        var firstEntry = Entry(RuntimeComponentKind.FrontendModule, ResolverExportForDifferentEntriesAAlias, TestAssemblyName);
        var secondEntry = Entry(RuntimeComponentKind.FrontendModule, ResolverExportForDifferentEntriesBAlias, TestAssemblyName);

        var first = resolver.Resolve(firstEntry);
        var second = resolver.Resolve(secondEntry);

        Assert.Multiple(() =>
        {
            Assert.That(first.ActivationType, Is.Not.EqualTo(second.ActivationType));
            Assert.That(first.Id, Is.Not.EqualTo(second.Id));
            Assert.That(strategy.GetCalls(TestAssemblyName), Is.EqualTo(1));
        });
    }

    [Test]
    public void Resolve_WhenAssemblyGetTypesThrowsReflectionTypeLoadException_UsesLoadableTypesFallback()
    {
        var fallbackAssembly = new ReflectionTypeLoadExceptionAssembly([typeof(ResolverExportForFallback), null, typeof(ResolverExportForDifferentEntriesA)]);
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = fallbackAssembly
        });
        var resolver = CreateResolver(strategy);
        var entry = Entry(RuntimeComponentKind.FrontendModule, ResolverExportForFallbackAlias, TestAssemblyName);

        var descriptor = resolver.Resolve(entry);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.ActivationType, Is.EqualTo(typeof(ResolverExportForFallback)));
            Assert.That(strategy.GetCalls(TestAssemblyName), Is.EqualTo(1));
        });
    }

    [Test]
    public void Resolve_WhenManifestKindDriftsFromExport_ThrowsDeterministicError()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateTestAssembly(typeof(ResolverExportForCaching))
        });
        var resolver = CreateResolver(strategy);
        var entry = new RuntimeComponentManifestEntry(
            RuntimeComponentKind.Backend,
            ResolverExportForCachingAlias,
            [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.FrontendModule, ResolverExportForCachingAlias),
            TestAssemblyName);

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(entry));

        Assert.That(
            ex!.Message,
            Does.Contain($"Runtime manifest entry '{entry.ComponentId}' resolves to type"));
        Assert.That(
            ex.Message,
            Does.Contain("but the exported component kind is 'FrontendModule' instead of 'Backend'."));
    }

    [Test]
    public void Resolve_WhenManifestCanonicalAliasDriftsFromExport_ThrowsDeterministicError()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateTestAssembly(typeof(ResolverExportForCaching))
        });
        var resolver = CreateResolver(strategy);
        var entry = new RuntimeComponentManifestEntry(
            RuntimeComponentKind.FrontendModule,
            ManifestAuthoritativeAlias,
            [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.FrontendModule, ResolverExportForCachingAlias),
            TestAssemblyName);

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(entry));

        Assert.That(
            ex!.Message,
            Does.Contain($"Runtime manifest entry '{entry.ComponentId}' resolves to type"));
        Assert.That(
            ex.Message,
            Does.Contain($"but the exported canonical alias is '{ResolverExportForCachingAlias}' instead of '{ManifestAuthoritativeAlias}'."));
    }

    [Test]
    public void Resolve_WhenActivationMetadataExists_LoadsExactTypeWithoutAssemblyScan()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateExactTypeAssembly(typeof(ResolverExactExport))
        });
        var resolver = CreateResolver(strategy);
        var entry = EntryWithActivation(RuntimeComponentKind.FrontendModule, ResolverExactExportAlias, TestAssemblyName, typeof(ResolverExactExport));

        var descriptor = resolver.Resolve(entry);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Id, Is.EqualTo(entry.ComponentId));
            Assert.That(descriptor.ActivationType, Is.EqualTo(typeof(ResolverExactExport)));
            Assert.That(strategy.GetCalls(TestAssemblyName), Is.EqualTo(1));
        });
    }

    [Test]
    public void Resolve_WhenActivationMetadataIsAbsent_UsesLegacyAssemblyScan()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateTestAssembly(typeof(ResolverExactExport))
        });
        var resolver = CreateResolver(strategy);
        var entry = Entry(RuntimeComponentKind.FrontendModule, ResolverExactExportAlias, TestAssemblyName);

        var descriptor = resolver.Resolve(entry);

        Assert.That(descriptor.ActivationType, Is.EqualTo(typeof(ResolverExactExport)));
    }

    [Test]
    public void Resolve_WhenExactActivationTypeIsMissing_ThrowsDeterministicError()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateExactTypeAssembly(typeof(ResolverExactExport))
        });
        var resolver = CreateResolver(strategy);
        var entry = new RuntimeComponentManifestEntry(
            RuntimeComponentKind.FrontendModule,
            ResolverExactExportAlias,
            [],
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.FrontendModule, ResolverExactExportAlias),
            TestAssemblyName,
            new RuntimeComponentActivationInfo("Missing.Runtime.Type"));

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(entry));

        Assert.That(
            ex!.Message,
            Is.EqualTo($"Runtime activation type 'Missing.Runtime.Type' was not found in assembly '{TestAssemblyName}'."));
    }

    [Test]
    public void Resolve_WhenExactActivationTypeHasNoRuntimeExport_ThrowsDeterministicError()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateExactTypeAssembly(typeof(ResolverExactWithoutExport))
        });
        var resolver = CreateResolver(strategy);
        var entry = EntryWithActivation(RuntimeComponentKind.FrontendModule, ResolverExactNoExportAlias, TestAssemblyName, typeof(ResolverExactWithoutExport));

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(entry));

        Assert.That(
            ex!.Message,
            Is.EqualTo($"Runtime activation type '{typeof(ResolverExactWithoutExport).FullName}' for manifest entry '{entry.ComponentId}' does not declare DialectRuntimeExportAttribute."));
    }

    [Test]
    public void Resolve_WhenExactActivationKindDriftsFromManifest_ThrowsDeterministicError()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateExactTypeAssembly(typeof(ResolverExactBackendExport))
        });
        var resolver = CreateResolver(strategy);
        var entry = EntryWithActivation(RuntimeComponentKind.FrontendModule, ResolverExactExportAlias, TestAssemblyName, typeof(ResolverExactBackendExport));

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(entry));

        Assert.That(
            ex!.Message,
            Does.Contain("but the exported component kind is 'Backend' instead of 'FrontendModule'."));
    }

    [Test]
    public void Resolve_WhenExactActivationCanonicalAliasDriftsFromManifest_ThrowsDeterministicError()
    {
        var strategy = new CountingAssemblyLoadStrategy(new Dictionary<string, Assembly>(StringComparer.Ordinal)
        {
            [TestAssemblyName] = CreateExactTypeAssembly(typeof(ResolverExactDifferentAliasExport))
        });
        var resolver = CreateResolver(strategy);
        var entry = EntryWithActivation(RuntimeComponentKind.FrontendModule, ResolverExactExportAlias, TestAssemblyName, typeof(ResolverExactDifferentAliasExport));

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(entry));

        Assert.That(
            ex!.Message,
            Does.Contain($"but the exported canonical alias is '{ManifestAuthoritativeAlias}' instead of '{ResolverExactExportAlias}'."));
    }

    private static RuntimeComponentManifestEntry Entry(RuntimeComponentKind kind, string canonicalAlias, string assemblySimpleName)
        => new(kind, canonicalAlias, [], RuntimeComponentIdFactory.Create(kind, canonicalAlias), assemblySimpleName);

    private static Assembly CreateTestAssembly(params Type[] loadableTypes) => new ReflectionTypeLoadExceptionAssembly(loadableTypes.Cast<Type?>().ToArray());

    private static Assembly CreateExactTypeAssembly(params Type[] loadableTypes) => new ExactTypeOnlyAssembly(loadableTypes);

    private static DefaultRuntimeComponentResolver CreateResolver(IRuntimeAssemblyLoadStrategy strategy)
        => new(new DefaultRuntimeAssemblyTypeLoader(strategy));

    private static RuntimeComponentManifestEntry EntryWithActivation(
        RuntimeComponentKind kind,
        string canonicalAlias,
        string assemblySimpleName,
        Type activationType) =>
        new(
            kind,
            canonicalAlias,
            [],
            RuntimeComponentIdFactory.Create(kind, canonicalAlias),
            assemblySimpleName,
            new RuntimeComponentActivationInfo(activationType.FullName!));

    [DialectRuntimeExport("FrontendModule", ResolverExportForCachingAlias)]
    private sealed class ResolverExportForCaching;

    [DialectRuntimeExport("FrontendModule", ResolverExportForAliasesAlias)]
    [DialectRuntimeAlias("beta")]
    [DialectRuntimeAlias(" Alpha ")]
    [DialectRuntimeAlias("alpha")]
    [DialectRuntimeAlias("beta")]
    private sealed class ResolverExportForAliases;

    [DialectRuntimeExport("FrontendModule", ResolverExportForDifferentEntriesAAlias)]
    private sealed class ResolverExportForDifferentEntriesA;

    [DialectRuntimeExport("FrontendModule", ResolverExportForDifferentEntriesBAlias)]
    private sealed class ResolverExportForDifferentEntriesB;

    [DialectRuntimeExport("FrontendModule", ResolverExportForFallbackAlias)]
    private sealed class ResolverExportForFallback;

    [DialectRuntimeExport("FrontendModule", ResolverExportDuplicateAlias)]
    private sealed class ResolverExportDuplicateA;

    [DialectRuntimeExport("FrontendModule", ResolverExportDuplicateAlias)]
    private sealed class ResolverExportDuplicateB;

    [DialectRuntimeExport("FrontendModule", ResolverExactExportAlias)]
    private sealed class ResolverExactExport;

    [DialectRuntimeExport("Backend", ResolverExactExportAlias)]
    private sealed class ResolverExactBackendExport;

    [DialectRuntimeExport("FrontendModule", ManifestAuthoritativeAlias)]
    private sealed class ResolverExactDifferentAliasExport;

    private sealed class ResolverExactWithoutExport;

    private sealed class CountingAssemblyLoadStrategy(IReadOnlyDictionary<string, Assembly> assemblies) : IRuntimeAssemblyLoadStrategy
    {
        private readonly IReadOnlyDictionary<string, Assembly> _assemblies = assemblies;
        private readonly ConcurrentDictionary<string, int> _calls = new(StringComparer.Ordinal);

        public Assembly LoadAssembly(string assemblySimpleName)
        {
            _calls.AddOrUpdate(assemblySimpleName, 1, static (_, count) => count + 1);

            if (_assemblies.TryGetValue(assemblySimpleName, out var assembly))
                return assembly;

            throw new FileNotFoundException($"Assembly '{assemblySimpleName}' is not configured for this test strategy.");
        }

        public int GetCalls(string assemblySimpleName) => _calls.TryGetValue(assemblySimpleName, out var calls) ? calls : 0;
    }

    private sealed class StubRuntimeComponentResolver(RuntimeComponentDescriptor descriptor) : IRuntimeComponentResolver
    {
        private readonly RuntimeComponentDescriptor _descriptor = descriptor;

        public RuntimeComponentDescriptor Resolve(RuntimeComponentManifestEntry entry) => _descriptor;
    }

    private sealed class ReflectionTypeLoadExceptionAssembly(Type?[] loadableTypes) : Assembly
    {
        private readonly Type?[] _loadableTypes = loadableTypes;

        public override Type[] GetTypes()
            => throw new ReflectionTypeLoadException(_loadableTypes, new Exception?[_loadableTypes.Length]);
    }

    private sealed class ExactTypeOnlyAssembly(IEnumerable<Type> loadableTypes) : Assembly
    {
        private readonly IReadOnlyDictionary<string, Type> _typesByFullName = loadableTypes
            .ToDictionary(static type => type.FullName!, StringComparer.Ordinal);

        public override Type? GetType(string name, bool throwOnError, bool ignoreCase)
        {
            if (!ignoreCase && _typesByFullName.TryGetValue(name, out var type))
                return type;

            return null;
        }

        public override Type[] GetTypes()
            => throw new InvalidOperationException("Exact activation path must not scan assembly exports.");
    }
}