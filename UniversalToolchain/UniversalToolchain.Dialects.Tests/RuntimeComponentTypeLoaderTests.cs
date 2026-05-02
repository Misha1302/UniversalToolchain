using ArithmeticModule.Module;

namespace UniversalToolchain.Dialects.Tests;

public class RuntimeComponentTypeLoaderTests
{
    [Test]
    public void TypeLoader_LoadsOnlyRequestedAssembly()
    {
        var loader = CreateLoader(new DefaultRuntimeAssemblyLoadStrategy(new DefaultRuntimeAssemblyLocator(new RuntimeArtifactLocatorOptions())));
        var entry = Entry("ArithmeticModule", "frontend.arithmetic");

        var type = loader.LoadType(entry);

        Assert.Multiple(() =>
        {
            Assert.That(type.FullName, Is.EqualTo("ArithmeticModule.Module.ArithmeticModuleImpl"));
            Assert.That(type.Assembly.GetName().Name, Is.EqualTo("ArithmeticModule"));
        });
    }

    [Test]
    public void TypeLoader_RepeatedLoad_UsesCache()
    {
        var loader = CreateLoader(new DefaultRuntimeAssemblyLoadStrategy(new DefaultRuntimeAssemblyLocator(new RuntimeArtifactLocatorOptions())));
        var entry = Entry("ArithmeticModule", "frontend.arithmetic");

        var first = loader.LoadType(entry);
        for (var i = 0; i < 20; i++)
        {
            var current = loader.LoadType(entry);
            Assert.That(ReferenceEquals(first, current), Is.True);
        }
    }

    [Test]
    public void TypeLoader_DoesNotUseLocator_WhenAssemblyAlreadyLoaded()
    {
        _ = typeof(ArithmeticModuleImpl).Assembly;
        var locator = new CountingLocator(false, null);
        var loader = CreateLoader(new DefaultRuntimeAssemblyLoadStrategy(locator));

        var type = loader.LoadType(Entry("ArithmeticModule", "frontend.arithmetic"));

        Assert.Multiple(() =>
        {
            Assert.That(type, Is.Not.Null);
            Assert.That(locator.Calls, Is.EqualTo(0));
        });
    }

    [Test]
    public void TypeLoader_UsesLocatorFallback_WhenLoadByNameFails()
    {
        var badAssembly = "DefinitelyMissing.Assembly.For.Loader.Test";
        var locator = new CountingLocator(true, Path.Combine(AppContext.BaseDirectory, "ArithmeticModule.dll"));
        var loader = CreateLoader(new DefaultRuntimeAssemblyLoadStrategy(locator));

        var type = loader.LoadType(Entry(badAssembly, "frontend.arithmetic"));

        Assert.Multiple(() =>
        {
            Assert.That(type.FullName, Is.EqualTo("ArithmeticModule.Module.ArithmeticModuleImpl"));
            Assert.That(locator.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void TypeLoader_LoadFromAssemblyPath_RequiresAbsolutePath()
    {
        var loader = CreateLoader(new DefaultRuntimeAssemblyLoadStrategy(new CountingLocator(true, "relative/path/ArithmeticModule.dll")));
        var ex = Assert.Throws<ArgumentException>(() => loader.LoadType(Entry("Missing.Assembly.With.Relative.Path", "frontend.arithmetic")));
        Assert.That(ex!.Message, Does.Contain("non-absolute path"));
    }

    [Test]
    public void TypeLoader_InvalidAssembly_ThrowsClearError()
    {
        var loader = CreateLoader(new DefaultRuntimeAssemblyLoadStrategy(new DefaultRuntimeAssemblyLocator(new RuntimeArtifactLocatorOptions())));
        var ex = Assert.Throws<FileNotFoundException>(() => loader.LoadType(Entry("NoSuchAssembly", "frontend.missing")));
        Assert.That(ex!.Message, Does.Contain("NoSuchAssembly"));
    }

    [Test]
    public void TypeLoader_InvalidType_ThrowsClearError()
    {
        var loader = CreateLoader(new DefaultRuntimeAssemblyLoadStrategy(new DefaultRuntimeAssemblyLocator(new RuntimeArtifactLocatorOptions())));
        Assert.Throws<InvalidOperationException>(() => loader.LoadType(Entry("ArithmeticModule", "frontend.missing")));
    }

    private static RuntimeComponentManifestEntry Entry(string assemblySimpleName, string componentId)
        => new(RuntimeComponentKind.FrontendModule, "Arithmetic", [], new RuntimeComponentId(componentId), assemblySimpleName);

    private static DefaultRuntimeComponentTypeLoader CreateLoader(IRuntimeAssemblyLoadStrategy strategy)
        => new(new DefaultRuntimeComponentResolver(new DefaultRuntimeAssemblyTypeLoader(strategy)));

    private sealed class CountingLocator(bool shouldResolve, string? path) : IRuntimeAssemblyLocator
    {
        public int Calls { get; private set; }

        public bool TryResolveAssemblyPath(string assemblySimpleName, out string? absolutePath)
        {
            Calls++;
            absolutePath = path;
            return shouldResolve;
        }
    }
}