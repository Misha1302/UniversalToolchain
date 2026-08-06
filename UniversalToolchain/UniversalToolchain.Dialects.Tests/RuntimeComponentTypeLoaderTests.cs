using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using BasicCore.Contracts;
using UniversalToolchain.Dialects.Abstractions;
using ArithmeticModule.Module;

namespace UniversalToolchain.Dialects.Tests;

public class RuntimeComponentTypeLoaderTests
{
    [Test]
    public void TypeLoader_LoadsOnlyRequestedAssembly()
    {
        var loader = CreateLoader(CreateStrategy(new DefaultRuntimeAssemblyLocator(new RuntimeArtifactLocatorOptions())));
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
        var loader = CreateLoader(CreateStrategy(new DefaultRuntimeAssemblyLocator(new RuntimeArtifactLocatorOptions())));
        var entry = Entry("ArithmeticModule", "frontend.arithmetic");

        var first = loader.LoadType(entry);
        for (var i = 0; i < 20; i++)
        {
            var current = loader.LoadType(entry);
            Assert.That(ReferenceEquals(first, current), Is.True);
        }
    }

    [Test]
    public void TypeLoader_UsesConfiguredRoot_WhenAssemblyWithSameIdentityIsAlreadyLoaded()
    {
        _ = typeof(ArithmeticModuleImpl).Assembly;
        var configuredPath = Path.Combine(AppContext.BaseDirectory, "ArithmeticModule.dll");
        var locator = new CountingLocator("ArithmeticModule", true, configuredPath);
        using var strategy = CreateStrategy(locator);
        var loader = CreateLoader(strategy);

        var type = loader.LoadType(Entry("ArithmeticModule", "frontend.arithmetic"));

        Assert.Multiple(() =>
        {
            Assert.That(type, Is.Not.Null);
            Assert.That(locator.Calls, Is.EqualTo(1));
            Assert.That(Path.GetFullPath(type.Assembly.Location), Is.EqualTo(Path.GetFullPath(configuredPath)));
        });
    }

    [Test]
    public void TypeLoader_RejectsConfiguredPathWhoseAssemblyIdentityDoesNotMatchRequest()
    {
        var badAssembly = "DefinitelyMissing.Assembly.For.Loader.Test";
        var locator = new CountingLocator(badAssembly, true, Path.Combine(AppContext.BaseDirectory, "ArithmeticModule.dll"));
        using var strategy = CreateStrategy(locator);
        var loader = CreateLoader(strategy);

        var exception = Assert.Throws<InvalidOperationException>(
            () => loader.LoadType(Entry(badAssembly, "frontend.arithmetic")));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("not requested assembly"));
            Assert.That(locator.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void TypeLoader_IgnoresSameIdentityAssemblyPreloadedFromDifferentContextAndPath()
    {
        var configuredPath = Path.Combine(AppContext.BaseDirectory, "ArithmeticModule.dll");
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), "wist-loader-hostile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
        var hostilePath = Path.Combine(temporaryDirectory, "ArithmeticModule.dll");
        File.Copy(configuredPath, hostilePath);

        try
        {
            AssertConfiguredPathWinsOverHostilePreload(hostilePath, configuredPath);
        }
        finally
        {
            DeleteDirectoryAfterCollectibleUnload(temporaryDirectory);
        }
    }

    [Test]
    public void TypeLoader_LoadFromAssemblyPath_RequiresAbsolutePath()
    {
        var loader = CreateLoader(CreateStrategy(new CountingLocator("Missing.Assembly.With.Relative.Path", true, "relative/path/ArithmeticModule.dll")));
        var ex = Assert.Throws<ArgumentException>(() => loader.LoadType(Entry("Missing.Assembly.With.Relative.Path", "frontend.arithmetic")));
        Assert.That(ex!.Message, Does.Contain("non-absolute path"));
    }

    [Test]
    public void TypeLoader_InvalidAssembly_ThrowsClearError()
    {
        var loader = CreateLoader(CreateStrategy(new DefaultRuntimeAssemblyLocator(new RuntimeArtifactLocatorOptions())));
        var ex = Assert.Throws<FileNotFoundException>(() => loader.LoadType(Entry("NoSuchAssembly", "frontend.missing")));
        Assert.That(ex!.Message, Does.Contain("NoSuchAssembly"));
    }

    [Test]
    public void TypeLoader_InvalidType_ThrowsClearError()
    {
        var loader = CreateLoader(CreateStrategy(new DefaultRuntimeAssemblyLocator(new RuntimeArtifactLocatorOptions())));
        Assert.Throws<InvalidOperationException>(() => loader.LoadType(Entry("ArithmeticModule", "frontend.missing")));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertConfiguredPathWinsOverHostilePreload(string hostilePath, string configuredPath)
    {
        var hostileContext = new AssemblyLoadContext("hostile-runtime-preload", isCollectible: true);
        try
        {
            var hostileAssembly = hostileContext.LoadFromAssemblyPath(hostilePath);
            var locator = new CountingLocator("ArithmeticModule", true, configuredPath);
            using var strategy = CreateStrategy(locator);
            var loader = CreateLoader(strategy);

            var type = loader.LoadType(Entry("ArithmeticModule", "frontend.arithmetic"));

            Assert.Multiple(() =>
            {
                Assert.That(type.Assembly, Is.Not.SameAs(hostileAssembly));
                Assert.That(Path.GetFullPath(type.Assembly.Location), Is.EqualTo(Path.GetFullPath(configuredPath)));
                Assert.That(locator.Calls, Is.EqualTo(1));
            });
        }
        finally
        {
            hostileContext.Unload();
        }
    }

    private static void DeleteDirectoryAfterCollectibleUnload(string directory)
    {
        for (var attempt = 0; attempt < 80; attempt++)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
                return;
            }
            catch (UnauthorizedAccessException) when (attempt < 79)
            {
            }
            catch (IOException) when (attempt < 79)
            {
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Sleep(10);
        }

        Directory.Delete(directory, recursive: true);
    }

    private static RuntimeComponentManifestEntry Entry(string assemblySimpleName, string componentId)
        => new(
            RuntimeComponentKind.FrontendModule,
            "Arithmetic",
            [],
            new RuntimeComponentId(componentId),
            assemblySimpleName,
            new RuntimeComponentActivationInfo(new RuntimeTypeReference(
                assemblySimpleName,
                componentId == "frontend.arithmetic"
                    ? typeof(ArithmeticModuleImpl).FullName!
                    : "Missing.Component.Type")));

    private static DefaultRuntimeAssemblyLoadStrategy CreateStrategy(IRuntimeAssemblyLocator locator) =>
        new(
            locator,
            new DefaultRuntimeSharedAssemblyResolver(
            [
                RuntimeSharedAssemblyDescriptor.Create(typeof(DialectRuntimeExportAttribute).Assembly),
                RuntimeSharedAssemblyDescriptor.Create(typeof(IFrontendCoreModule).Assembly)
            ]));

    private static DefaultRuntimeComponentTypeLoader CreateLoader(IRuntimeAssemblyLoadStrategy strategy)
        => new(new DefaultRuntimeComponentResolver(new DefaultRuntimeAssemblyTypeLoader(strategy)));

    private sealed class CountingLocator(
        string configuredAssemblySimpleName,
        bool shouldResolve,
        string? path) : IRuntimeAssemblyLocator
    {
        private readonly DefaultRuntimeAssemblyLocator _fallback = new(new RuntimeArtifactLocatorOptions());

        public int Calls { get; private set; }

        public bool TryResolveAssemblyPath(string assemblySimpleName, out string? absolutePath)
        {
            if (string.Equals(assemblySimpleName, configuredAssemblySimpleName, StringComparison.Ordinal))
            {
                Calls++;
                absolutePath = path;
                return shouldResolve;
            }

            return _fallback.TryResolveAssemblyPath(assemblySimpleName, out absolutePath);
        }
    }
}
