using System.Reflection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests.Integration;

public class DefaultRuntimeComponentResolverTests
{
    private const string FakeCanonicalAlias = "fake-module";
    private const string ManifestAlias = "manifest-alias";
    private const string OtherCanonicalAlias = "other-module";
    private const string ReflectionLegacyAlias = "reflection-legacy-alias";

    [Test]
    public void Resolve_WhenManifestMatchesExport_ReturnsManifestAuthoritativeDescriptor()
    {
        var resolver = new DefaultRuntimeComponentResolver(new FakeAssemblyLoadStrategy());
        var entry = Entry(
            RuntimeComponentKind.FrontendModule,
            FakeCanonicalAlias,
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.FrontendModule, FakeCanonicalAlias),
            [ManifestAlias]);

        var descriptor = resolver.Resolve(entry);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.ActivationType, Is.EqualTo(typeof(FakeFrontendModuleExport)));
            Assert.That(descriptor.Id, Is.EqualTo(entry.ComponentId));
            Assert.That(descriptor.Kind, Is.EqualTo(entry.Kind));
            Assert.That(descriptor.CanonicalAlias, Is.EqualTo(entry.CanonicalAlias));
            Assert.That(descriptor.Aliases, Is.EqualTo(entry.Aliases));
            Assert.That(descriptor.Aliases, Does.Not.Contain(ReflectionLegacyAlias));
        });
    }

    [Test]
    public void Resolve_WhenManifestKindDoesNotMatchExport_Throws()
    {
        var resolver = new DefaultRuntimeComponentResolver(new FakeAssemblyLoadStrategy());
        var entry = Entry(
            RuntimeComponentKind.Backend,
            FakeCanonicalAlias,
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.FrontendModule, FakeCanonicalAlias));

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(entry));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.Not.Empty);
            Assert.That(ex.Message, Does.Contain("exported component kind"));
            Assert.That(ex.Message, Does.Contain("FrontendModule"));
            Assert.That(ex.Message, Does.Contain("Backend"));
        });
    }

    [Test]
    public void Resolve_WhenManifestCanonicalAliasDoesNotMatchExport_Throws()
    {
        var resolver = new DefaultRuntimeComponentResolver(new FakeAssemblyLoadStrategy());
        var entry = Entry(
            RuntimeComponentKind.FrontendModule,
            OtherCanonicalAlias,
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.FrontendModule, FakeCanonicalAlias));

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(entry));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.Not.Empty);
            Assert.That(ex.Message, Does.Contain("exported canonical alias"));
            Assert.That(ex.Message, Does.Contain(FakeCanonicalAlias));
            Assert.That(ex.Message, Does.Contain(OtherCanonicalAlias));
        });
    }

    [Test]
    public void Resolve_WhenComponentIsNotExported_ThrowsDeterministicMessage()
    {
        var resolver = new DefaultRuntimeComponentResolver(new FakeAssemblyLoadStrategy());
        var entry = Entry(
            RuntimeComponentKind.FrontendModule,
            "missing-module",
            RuntimeComponentIdFactory.Create(RuntimeComponentKind.FrontendModule, "missing-module"));

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(entry));

        Assert.That(
            ex!.Message,
            Is.EqualTo($"Runtime component '{entry.ComponentId}' was not found in assembly '{entry.AssemblySimpleName}'."));
    }

    private static RuntimeComponentManifestEntry Entry(
        RuntimeComponentKind kind,
        string canonicalAlias,
        RuntimeComponentId componentId,
        IReadOnlyList<string>? aliases = null)
    {
        return new RuntimeComponentManifestEntry(
            kind,
            canonicalAlias,
            aliases ?? [],
            componentId,
            typeof(FakeFrontendModuleExport).Assembly.GetName().Name!);
    }

    [DialectRuntimeExport("FrontendModule", FakeCanonicalAlias)]
    [DialectRuntimeAlias(ReflectionLegacyAlias)]
    private sealed class FakeFrontendModuleExport;

    private sealed class FakeAssemblyLoadStrategy : IRuntimeAssemblyLoadStrategy
    {
        private readonly Assembly _assembly = new FakeRuntimeAssembly();

        public Assembly LoadAssembly(string assemblySimpleName)
        {
            return _assembly;
        }
    }

    private sealed class FakeRuntimeAssembly : Assembly
    {
        public override Type[] GetTypes() => [typeof(FakeFrontendModuleExport)];
    }
}
