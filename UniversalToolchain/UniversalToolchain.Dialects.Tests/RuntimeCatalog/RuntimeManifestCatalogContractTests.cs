using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests.RuntimeCatalog;

public class RuntimeManifestCatalogContractTests
{
    [Test]
    public void LoadEntries_ShouldReadManifestFilesInDeterministicOrder()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();

        var zPath = WriteManifest(temp.Path, "z.dialect.runtime.json", "ZAssembly", [Module("zeta", "Z.Type")], serializer);
        var aPath = WriteManifest(temp.Path, "a.dialect.runtime.json", "AAssembly", [Module("alpha", "A.Type")], serializer);

        var catalog = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([zPath, aPath]), serializer);

        Assert.That(catalog.GetModulesInDeterministicOrder().Select(static x => x.TypeReference.AssemblySimpleName), Is.EqualTo(new[] { "AAssembly", "ZAssembly" }));
    }

    [Test]
    public void LoadEntries_ShouldRejectEmptyAssemblySimpleName()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var path = WriteManifest(temp.Path, "empty-asm.dialect.runtime.json", "  ", [Module("Arithmetic", "Arithmetic.Type")], serializer);

        var ex = Assert.Throws<ArgumentException>(() => new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([path]), serializer));
        Assert.That(ex!.Message, Does.Contain("empty assemblySimpleName"));
    }

    [Test]
    public void LoadEntries_ShouldRejectEmptyCanonicalAlias()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var path = WriteManifest(temp.Path, "empty-canonical.dialect.runtime.json", "Asm", [Module("  ", "Arithmetic.Type")], serializer);

        var ex = Assert.Throws<ArgumentException>(() => new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([path]), serializer));
        Assert.That(ex!.Message, Does.Contain("Canonical alias must not be empty"));
    }

    [Test]
    public void LoadEntries_ShouldRejectEmptyTypeFullName()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var path = WriteManifest(temp.Path, "empty-type.dialect.runtime.json", "Asm", [Module("Arithmetic", " ")], serializer);

        var ex = Assert.Throws<ArgumentException>(() => new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([path]), serializer));
        Assert.That(ex!.Message, Does.Contain("TypeReference.TypeFullName must not be empty"));
    }

    [Test]
    public void Catalog_ShouldRejectDuplicateModuleAlias()
    {
        var ex = BuildDuplicateAliasCatalogException(
            Module("Arithmetic", "A.Type", "common"),
            Module("Numbers", "B.Type", "common"));

        Assert.That(ex.Message, Does.Contain("Duplicate runtime module alias 'common'"));
    }

    [Test]
    public void Catalog_ShouldRejectDuplicateOptimizerAlias()
    {
        var ex = BuildDuplicateAliasCatalogException(
            Optimizer("OptA", "OptA.Type", "common"),
            Optimizer("OptB", "OptB.Type", "common"));

        Assert.That(ex.Message, Does.Contain("Duplicate runtime optimizer alias 'common'"));
    }

    [Test]
    public void Catalog_ShouldRejectDuplicateBackendAlias()
    {
        var ex = BuildDuplicateAliasCatalogException(
            Backend("compiler", "Compiler.Type", "runtime"),
            Backend("interpreter", "Interpreter.Type", "runtime"));

        Assert.That(ex.Message, Does.Contain("Duplicate runtime backend alias 'runtime'"));
    }

    [Test]
    public void Catalog_ShouldNormalizeAndSortAliasesDeterministically()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var path = WriteManifest(
            temp.Path,
            "aliases.dialect.runtime.json",
            "Asm",
            [Module("  Arithmetic  ", "  Arithmetic.Module.Type ", " b ", "Arithmetic", "a", "b", "  ")],
            serializer);

        var catalog = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([path]), serializer);
        Assert.That(catalog.TryResolveModule("Arithmetic", out var entry), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(entry!.CanonicalAlias, Is.EqualTo("Arithmetic"));
            Assert.That(entry.Aliases, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(entry.TypeReference.TypeFullName, Is.EqualTo("Arithmetic.Module.Type"));
        });
    }

    private static InvalidOperationException BuildDuplicateAliasCatalogException(
        FileDialectRuntimeComponentEntry first,
        FileDialectRuntimeComponentEntry second)
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var firstPath = WriteManifest(temp.Path, "first.dialect.runtime.json", "AAssembly", [first], serializer);
        var secondPath = WriteManifest(temp.Path, "second.dialect.runtime.json", "BAssembly", [second], serializer);

        return Assert.Throws<InvalidOperationException>(() => new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([firstPath, secondPath]), serializer))!;
    }

    private static FileDialectRuntimeComponentEntry Module(string alias, string type, params string[] aliases) =>
        new("FrontendModule", alias, aliases, type);

    private static FileDialectRuntimeComponentEntry Optimizer(string alias, string type, params string[] aliases) =>
        new("Optimizer", alias, aliases, type);

    private static FileDialectRuntimeComponentEntry Backend(string alias, string type, params string[] aliases) =>
        new("Backend", alias, aliases, type);

    private static string WriteManifest(string root, string fileName, string assemblySimpleName, IReadOnlyList<FileDialectRuntimeComponentEntry> components, IRuntimeManifestSerializer serializer)
    {
        var path = Path.Combine(root, fileName);
        var document = new FileDialectRuntimeManifestDocument(assemblySimpleName, components);
        File.WriteAllText(path, serializer.Serialize(document));
        return path;
    }

    private sealed class StaticManifestLocator(IReadOnlyList<string> paths) : IRuntimeManifestFileLocator
    {
        public IReadOnlyList<string> GetManifestFilePaths() => paths;
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dialect-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
