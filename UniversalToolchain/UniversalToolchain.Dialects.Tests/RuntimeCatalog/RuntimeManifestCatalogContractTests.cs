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

        Assert.That(catalog.GetModulesInDeterministicOrder().Select(static x => x.AssemblySimpleName), Is.EqualTo(new[] { "AAssembly", "ZAssembly" }));
    }

    [Test]
    public void LoadEntries_ShouldRejectEmptyAssemblySimpleName()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var path = WriteManifest(temp.Path, "empty-asm.dialect.runtime.json", "  ", [Module("Arithmetic", "Arithmetic.Type")], serializer);

        var ex = Assert.Throws<InvalidOperationException>(() => _ = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([path]), serializer));
        Assert.That(ex!.Message, Does.Contain("must declare assemblySimpleName"));
    }

    [Test]
    public void LoadEntries_ShouldRejectEmptyCanonicalAlias()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var path = WriteManifest(temp.Path, "empty-canonical.dialect.runtime.json", "Asm", [Module("  ", "Arithmetic.Type")], serializer);

        var ex = Assert.Throws<ArgumentException>(() => _ = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([path]), serializer));
        Assert.That(ex!.Message, Does.Contain("Canonical alias must not be empty"));
    }

    [Test]
    public void LoadEntries_ShouldRejectMissingComponentId()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var path = WriteManifest(temp.Path, "empty-type.dialect.runtime.json", "Asm", [new FileDialectRuntimeComponentEntry("FrontendModule", "Arithmetic", [], " ", new FileRuntimeComponentActivationEntry(new RuntimeTypeReference("Asm", "Arithmetic.Type")))], serializer);

        var ex = Assert.Throws<InvalidOperationException>(() => _ = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([path]), serializer));
        Assert.That(ex!.Message, Does.Contain("must declare componentId"));
    }

    [Test]
    public void LoadEntries_ManifestWithoutActivationMetadata_IsRejected()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var path = WriteRawManifest(
            temp.Path,
            "without-activation.dialect.runtime.json",
            """
            {"assemblySimpleName":"Asm","components":[{"kind":"FrontendModule","canonicalAlias":"Arithmetic","aliases":[],"componentId":"frontend.arithmetic"}]}
            """);

        var ex = Assert.Throws<InvalidOperationException>(() => _ = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([path]), serializer));
        Assert.That(ex!.Message, Does.Contain("must declare activation.activationType"));
    }

    [Test]
    public void LoadEntries_ManifestWithActivationMetadata_AcceptsEntry()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var path = WriteRawManifest(
            temp.Path,
            "with-activation.dialect.runtime.json",
            """
            {"assemblySimpleName":"Asm","components":[{"kind":"FrontendModule","canonicalAlias":"Arithmetic","aliases":[],"componentId":"frontend.arithmetic","activation":{"activationType":{"assemblySimpleName":"Asm","typeFullName":"Modules.ArithmeticModule"},"registrarType":{"assemblySimpleName":"Asm","typeFullName":"Modules.ArithmeticRegistrar"}}}]}
            """);

        var catalog = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([path]), serializer);

        Assert.That(catalog.TryResolveModule("Arithmetic", out var entry), Is.True);
        Assert.That(entry!.Activation, Is.Not.Null);
    }

    [Test]
    public void LoadEntries_ManifestWithActivationMetadata_PreservesActivationMetadata()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var path = WriteManifest(
            temp.Path,
            "preserve-activation.dialect.runtime.json",
            "Asm",
            [
                new FileDialectRuntimeComponentEntry(
                    "FrontendModule",
                    "Arithmetic",
                    [],
                    "frontend.arithmetic",
                    new FileRuntimeComponentActivationEntry(
                        new RuntimeTypeReference("Asm", "Modules.ArithmeticModule"),
                        new RuntimeTypeReference("Asm", "Modules.ArithmeticRegistrar")))
            ],
            serializer);

        var catalog = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([path]), serializer);
        Assert.That(catalog.TryResolveModule("Arithmetic", out var entry), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(entry!.Activation!.ActivationTypeFullName, Is.EqualTo("Modules.ArithmeticModule"));
            Assert.That(entry.Activation.ActivationAssemblySimpleName, Is.EqualTo("Asm"));
            Assert.That(entry.Activation.RegistrarTypeFullName, Is.EqualTo("Modules.ArithmeticRegistrar"));
            Assert.That(entry.Activation.RegistrarAssemblySimpleName, Is.EqualTo("Asm"));
        });
    }

    [Test]
    public void LoadEntries_ManifestWithStructuredActivationMetadata_PreservesAssemblyIdentity()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var path = WriteRawManifest(
            temp.Path,
            "structured-activation.dialect.runtime.json",
            """
            {"assemblySimpleName":"OwnerAssembly","components":[{"kind":"Backend","canonicalAlias":"interpreter","aliases":[],"componentId":"backend.interpreter","activation":{"activationType":{"assemblySimpleName":"DeclarationAssembly","typeFullName":"Runtime.InterpreterDeclaration"},"registrarType":{"assemblySimpleName":"RegistrarAssembly","typeFullName":"Runtime.InterpreterRegistrar"}}}]}
            """);

        var catalog = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([path]), serializer);
        Assert.That(catalog.TryResolveBackend("interpreter", out var entry), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(entry!.Activation!.ActivationAssemblySimpleName, Is.EqualTo("DeclarationAssembly"));
            Assert.That(entry.Activation.ActivationTypeFullName, Is.EqualTo("Runtime.InterpreterDeclaration"));
            Assert.That(entry.Activation.RegistrarAssemblySimpleName, Is.EqualTo("RegistrarAssembly"));
            Assert.That(entry.Activation.RegistrarTypeFullName, Is.EqualTo("Runtime.InterpreterRegistrar"));
        });
    }

    [Test]
    public void LoadEntries_ActivationMetadataHasEmptyActivationTypeFullName_Throws()
    {
        using var temp = new TempDirectory();
        var serializer = new RuntimeManifestJsonSerializer();
        var path = WriteRawManifest(
            temp.Path,
            "empty-activation-type.dialect.runtime.json",
            """
            {"assemblySimpleName":"Asm","components":[{"kind":"FrontendModule","canonicalAlias":"Arithmetic","aliases":[],"componentId":"frontend.arithmetic","activation":{"activationType":{"assemblySimpleName":"Asm","typeFullName":" "}}}]}
            """);

        var ex = Assert.Throws<InvalidOperationException>(() => _ = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([path]), serializer));
        Assert.That(ex!.Message, Does.Contain("must declare exact assemblySimpleName and typeFullName"));
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
            Backend("cil", "Compiler.Type", "runtime"),
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
            Assert.That(entry.ComponentId.Value, Is.EqualTo("frontend.arithmetic"));
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

        return Assert.Throws<InvalidOperationException>(() => _ = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([firstPath, secondPath]), serializer))!;
    }

    private static FileDialectRuntimeComponentEntry Module(string alias, string typeFullName, params string[] aliases) =>
        Entry(RuntimeComponentKind.FrontendModule, "FrontendModule", alias, typeFullName, aliases);

    private static FileDialectRuntimeComponentEntry Optimizer(string alias, string typeFullName, params string[] aliases) =>
        Entry(RuntimeComponentKind.Optimizer, "Optimizer", alias, typeFullName, aliases);

    private static FileDialectRuntimeComponentEntry Backend(string alias, string typeFullName, params string[] aliases) =>
        Entry(RuntimeComponentKind.Backend, "Backend", alias, typeFullName, aliases);

    private static FileDialectRuntimeComponentEntry Entry(
        RuntimeComponentKind kind,
        string kindText,
        string alias,
        string typeFullName,
        IReadOnlyList<string> aliases)
    {
        var normalizedAlias = alias.Trim();
        var assemblySimpleName = typeFullName.Contains('.', StringComparison.Ordinal)
            ? typeFullName[..typeFullName.IndexOf('.', StringComparison.Ordinal)]
            : "TestAssembly";

        return new FileDialectRuntimeComponentEntry(
            kindText,
            alias,
            aliases,
            RuntimeComponentIdFactory.Create(kind, normalizedAlias).Value,
            new FileRuntimeComponentActivationEntry(
                new RuntimeTypeReference(assemblySimpleName, typeFullName.Trim())));
    }

    private static string WriteManifest(string root, string fileName, string assemblySimpleName, IReadOnlyList<FileDialectRuntimeComponentEntry> components, IRuntimeManifestSerializer serializer)
    {
        var path = Path.Combine(root, fileName);
        var document = new FileDialectRuntimeManifestDocument(assemblySimpleName, components);
        File.WriteAllText(path, serializer.Serialize(document));
        return path;
    }

    private static string WriteRawManifest(string root, string fileName, string json)
    {
        var path = Path.Combine(root, fileName);
        File.WriteAllText(path, json);
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
                Directory.Delete(Path, true);
        }
    }
}