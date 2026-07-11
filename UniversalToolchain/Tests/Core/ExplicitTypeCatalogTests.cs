using System.Reflection;
using AssemblyFinder;

namespace Tests.Core;

[TestFixture]
public sealed class ExplicitTypeCatalogTests
{
    [Test]
    public void ImmutableCatalog_ContainsOnlyExplicitAssemblies()
    {
        var catalog = new ImmutableTypeCatalog([typeof(ExplicitTypeCatalogTests).Assembly]);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.Assemblies, Is.EqualTo(new[] { typeof(ExplicitTypeCatalogTests).Assembly }));
            Assert.That(catalog.Types, Does.Contain(typeof(ExplicitTypeCatalogTests)));
            Assert.That(catalog.Types, Does.Not.Contain(typeof(Console)));
        });
    }

    [Test]
    public void ResolveRequiredType_WhenShortNameIsAmbiguous_FailsDeterministically()
    {
        var catalog = new ImmutableTypeCatalog([typeof(ExplicitTypeCatalogTests).Assembly]);

        var exception = Assert.Throws<AmbiguousMatchException>(() => catalog.ResolveRequiredType("Duplicate"));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain(typeof(CatalogCollisionA.Duplicate).FullName));
            Assert.That(exception.Message, Does.Contain(typeof(CatalogCollisionB.Duplicate).FullName));
        });
    }

    [Test]
    public void MethodResolver_ShouldKeepSupportedGenericStandardLibraryMethods()
    {
        var catalog = TypeCatalogFactory.Create([typeof(BasicStdLib.Main).Assembly]);
        var resolver = new DeterministicMethodResolver(catalog);

        var method = resolver.GetMethod("Main.Round", [typeof(NumbersModule.Core.RealNumberImpl)]);

        Assert.Multiple(() =>
        {
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.IsGenericMethodDefinition, Is.False);
            Assert.That(method.IsGenericMethod, Is.True);
            Assert.That(method.GetGenericArguments(), Is.EqualTo(new[] { typeof(NumbersModule.Core.RealNumberImpl) }));
        });
    }

    [Test]
    public void TypeCatalogFactory_DoesNotObserveAssembliesLoadedAfterConstruction()
    {
        var catalog = TypeCatalogFactory.Create(Array.Empty<Assembly>());
        var snapshot = catalog.Assemblies.ToArray();

        _ = typeof(ExplicitTypeCatalogTests).Assembly.GetTypes();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot, Is.Empty, "The compatibility factory must fail closed without an explicit allowlist.");
            Assert.That(catalog.Assemblies, Is.EqualTo(snapshot));
        });
    }
}

internal static class CatalogCollisionA
{
    internal sealed class Duplicate { }
}

internal static class CatalogCollisionB
{
    internal sealed class Duplicate { }
}
