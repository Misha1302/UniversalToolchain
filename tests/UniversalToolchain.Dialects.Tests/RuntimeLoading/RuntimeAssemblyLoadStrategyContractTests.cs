using System.Reflection;
using ArithmeticModule.Module;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class RuntimeAssemblyLoadStrategyContractTests
{
    [Test]
    public void TypeLoader_ShouldUseInjectedAssemblyLoadStrategy()
    {
        var strategy = new CountingAssemblyLoadStrategy(typeof(ArithmeticModuleImpl).Assembly);
        var loader = new DefaultRuntimeComponentTypeLoader(new DefaultRuntimeComponentResolver(strategy));

        var resolved = loader.LoadType(Entry("ArithmeticModule", "frontend.arithmetic"));

        Assert.Multiple(() =>
        {
            Assert.That(resolved.FullName, Is.EqualTo("ArithmeticModule.Module.ArithmeticModuleImpl"));
            Assert.That(strategy.Calls, Is.EqualTo(1));
            Assert.That(strategy.CalledAssemblySimpleNames, Is.EqualTo(new[] { "ArithmeticModule" }));
        });
    }

    [Test]
    public void TypeLoader_ShouldThrowClearError_ForMissingAssembly()
    {
        var strategy = new ThrowingAssemblyLoadStrategy(new FileNotFoundException("Assembly 'Missing.Assembly' was not found."));
        var loader = new DefaultRuntimeComponentTypeLoader(new DefaultRuntimeComponentResolver(strategy));

        var ex = Assert.Throws<FileNotFoundException>(() => loader.LoadType(Entry("Missing.Assembly", "frontend.missing")));

        Assert.That(ex!.Message, Does.Contain("Missing.Assembly"));
    }

    [Test]
    public void TypeLoader_ShouldThrowClearError_ForMissingTypeInAssembly()
    {
        var strategy = new CountingAssemblyLoadStrategy(typeof(ArithmeticModuleImpl).Assembly);
        var loader = new DefaultRuntimeComponentTypeLoader(new DefaultRuntimeComponentResolver(strategy));

        var ex = Assert.Throws<InvalidOperationException>(() => loader.LoadType(Entry("ArithmeticModule", "frontend.missing")));

        Assert.That(ex!.Message, Does.Contain("frontend.missing"));
    }

    [Test]
    public void TypeLoader_ShouldCacheResolvedTypeDeterministically()
    {
        var strategy = new CountingAssemblyLoadStrategy(typeof(ArithmeticModuleImpl).Assembly);
        var loader = new DefaultRuntimeComponentTypeLoader(new DefaultRuntimeComponentResolver(strategy));
        var entry = Entry("ArithmeticModule", "frontend.arithmetic");

        var first = loader.LoadType(entry);
        var second = loader.LoadType(entry);

        Assert.Multiple(() =>
        {
            Assert.That(ReferenceEquals(first, second), Is.True);
            Assert.That(strategy.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void TypeLoader_ShouldReturnSameType_ForRepeatedResolutions()
    {
        var strategy = new CountingAssemblyLoadStrategy(typeof(ArithmeticModuleImpl).Assembly);
        var loader = new DefaultRuntimeComponentTypeLoader(new DefaultRuntimeComponentResolver(strategy));
        var entry = Entry("ArithmeticModule", "frontend.arithmetic");

        var baseline = loader.LoadType(entry);
        for (var i = 0; i < 50; i++)
            Assert.That(loader.LoadType(entry), Is.SameAs(baseline));
    }

    [Test]
    public async Task TypeLoader_ShouldBeSafe_ForParallelResolutionsOfSameType()
    {
        var strategy = new CountingAssemblyLoadStrategy(typeof(ArithmeticModuleImpl).Assembly);
        var loader = new DefaultRuntimeComponentTypeLoader(new DefaultRuntimeComponentResolver(strategy));
        var entry = Entry("ArithmeticModule", "frontend.arithmetic");

        var tasks = Enumerable.Range(0, 64).Select(_ => Task.Run(() => loader.LoadType(entry))).ToArray();
        var types = await Task.WhenAll(tasks);

        Assert.Multiple(() =>
        {
            Assert.That(types.Select(static x => x.AssemblyQualifiedName).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
            Assert.That(strategy.Calls, Is.EqualTo(1));
        });
    }

    private static RuntimeComponentManifestEntry Entry(string assemblySimpleName, string componentId) =>
        new(RuntimeComponentKind.FrontendModule, "Arithmetic", [], new RuntimeComponentId(componentId), assemblySimpleName);

    private sealed class CountingAssemblyLoadStrategy(Assembly assembly) : IRuntimeAssemblyLoadStrategy
    {
        private readonly Assembly _assembly = assembly;

        public int Calls { get; private set; }
        public List<string> CalledAssemblySimpleNames { get; } = [];

        public Assembly LoadAssembly(string assemblySimpleName)
        {
            Calls++;
            CalledAssemblySimpleNames.Add(assemblySimpleName);
            return _assembly;
        }
    }

    private sealed class ThrowingAssemblyLoadStrategy(Exception exception) : IRuntimeAssemblyLoadStrategy
    {
        public Assembly LoadAssembly(string assemblySimpleName) => throw exception;
    }
}