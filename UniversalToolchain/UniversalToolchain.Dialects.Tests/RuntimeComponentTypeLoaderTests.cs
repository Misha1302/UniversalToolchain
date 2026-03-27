using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class RuntimeComponentTypeLoaderTests
{
    [Test]
    public void TypeLoader_LoadsOnlyRequestedAssembly()
    {
        var loader = new DefaultRuntimeComponentTypeLoader();
        var entry = new RuntimeComponentManifestEntry(RuntimeComponentKind.FrontendModule, "Arithmetic", [], "ArithmeticModule", "ArithmeticModule.Module.ArithmeticModuleImpl");

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
        var loader = new DefaultRuntimeComponentTypeLoader();
        var entry = new RuntimeComponentManifestEntry(RuntimeComponentKind.FrontendModule, "Arithmetic", [], "ArithmeticModule", "ArithmeticModule.Module.ArithmeticModuleImpl");

        var first = loader.LoadType(entry);
        for (var i = 0; i < 20; i++)
        {
            var current = loader.LoadType(entry);
            Assert.That(ReferenceEquals(first, current), Is.True);
        }
    }

    [Test]
    public void TypeLoader_InvalidAssembly_ThrowsClearError()
    {
        var loader = new DefaultRuntimeComponentTypeLoader();
        var entry = new RuntimeComponentManifestEntry(RuntimeComponentKind.FrontendModule, "Bad", [], "NoSuchAssembly", "Missing.Type");
        var ex = Assert.Throws<FileNotFoundException>(() => loader.LoadType(entry));
        Assert.That(ex!.Message, Does.Contain("NoSuchAssembly.dll"));
    }

    [Test]
    public void TypeLoader_InvalidType_ThrowsClearError()
    {
        var loader = new DefaultRuntimeComponentTypeLoader();
        var entry = new RuntimeComponentManifestEntry(RuntimeComponentKind.FrontendModule, "Bad", [], "ArithmeticModule", "Missing.Type");
        Assert.Throws<TypeLoadException>(() => loader.LoadType(entry));
    }
}
