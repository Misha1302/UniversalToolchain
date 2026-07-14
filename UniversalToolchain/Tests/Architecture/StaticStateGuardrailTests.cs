using System.Text.RegularExpressions;

namespace Tests.Architecture;

[TestFixture]
public sealed class StaticStateGuardrailTests
{
    private static readonly Regex MutableStaticCollectionFieldRegex = new(
        @"(?m)^\s*(?:private|internal|protected|public)?\s*static\s+(?:readonly\s+)?(?:Dictionary|List|HashSet|ConcurrentDictionary)\s*<.+>\s+[_a-zA-Z][_a-zA-Z0-9]*\s*(?:=|;)|^\s*(?:private|internal|protected|public)?\s*static\s+(?:readonly\s+)?OrderedDictionary\s+[_a-zA-Z][_a-zA-Z0-9]*\s*(?:=|;)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] GuardedProductionDirectories =
    [
        "UniversalToolchain/BasicCore/",
        "UniversalToolchain/BasicInterpreter/",
        "UniversalToolchain/BytecodeDynamicMethodsCompiler/",
        "UniversalToolchain/NativeMathModule/",
        "UniversalToolchain/AssemblyFinder/",
        "UniversalToolchain/DynamicMethodCalling/",
        "UniversalToolchain/ObjectExtensions/",
        "UniversalToolchain/CSharpInteropModule/",
        "UniversalToolchain/VariablesModule/",
        "UniversalToolchain/UniversalToolchain.Wist/"
    ];

    private static readonly string[] AllowedCurrentDebtFiles =
    [
        "UniversalToolchain/NativeMathModule/NativeArithmeticAstVisitor.cs",
        "UniversalToolchain/NativeMathModule/NativeCILOptimizerModule.cs"
    ];

    [Test]
    public void CriticalRuntimeProjects_ShouldNotIntroduceNewMutableStaticCollectionFields()
    {
        var root = FindRepositoryRoot();
        var universalToolchainRoot = Path.Combine(root, "UniversalToolchain");
        var files = Directory.GetFiles(universalToolchainRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => IsGuardedProductionSourceFile(root, path))
            .Where(path => !IsAllowedCurrentDebtFile(root, path))
            .ToList();

        var violations = files
            .SelectMany(path => FindViolations(root, path))
            .ToList();

        Assert.That(
            violations,
            Is.Empty,
            "Mutable static collection fields create hidden process-wide state. Add an explicit architectural exception only for known legacy debt.");
    }

    [Test]
    public void DiscoveryAndDynamicMethodLifetimeBoundaries_ShouldRemainProcessLocalAndExplicit()
    {
        var root = FindRepositoryRoot();
        var relevantFiles = new[]
        {
            "UniversalToolchain/AssemblyFinder/ImmutableTypeCatalog.cs",
            "UniversalToolchain/AssemblyFinder/TypeCatalogFactory.cs",
            "UniversalToolchain/DynamicMethodCalling/Core/DynamicMethodInvokerBase.cs",
            "UniversalToolchain/ObjectExtensions/ObjectExtension.cs"
        };

        var source = string.Join(
            Environment.NewLine,
            relevantFiles.Select(path => File.ReadAllText(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)))));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Not.Contain("AppDomain.CurrentDomain"));
            Assert.That(source, Does.Not.Contain("AppContext.BaseDirectory"));
            Assert.That(source, Does.Not.Contain("SearchOption.AllDirectories"));
            Assert.That(source, Does.Not.Contain("LoadFromAssemblyPath"));
            Assert.That(source, Does.Not.Contain("MakeImmortal"));
            Assert.That(source, Does.Not.Contain("_immortalObjects"));
        });
    }

    [Test]
    public void VendoredPackageFeed_ShouldContainOnlyCanonicalRootNugetPackages()
    {
        var root = FindRepositoryRoot();
        var packageRoot = Path.Combine(root, "UniversalToolchain", "packages");
        var files = Directory.GetFiles(packageRoot, "*", SearchOption.AllDirectories);

        var violations = files
            .Select(path => NormalizePath(Path.GetRelativePath(packageRoot, path)))
            .Where(relativePath => relativePath.Contains('/', StringComparison.Ordinal)
                                   || !relativePath.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.That(
            violations,
            Is.Empty,
            "The vendored feed must contain only canonical root-level .nupkg files; cache artifacts and opaque extensions are forbidden.");
    }

    [Test]
    public void KnownCriticalRuntimeMutableStaticCollectionDebt_ShouldRemainExplicitlyTracked()
    {
        var root = FindRepositoryRoot();
        var missingDebtMarkers = AllowedCurrentDebtFiles
            .Select(path => Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)))
            .Where(File.Exists)
            .Where(path => !FindViolations(root, path).Any())
            .Select(path => NormalizePath(Path.GetRelativePath(root, path)))
            .ToList();

        Assert.That(
            missingDebtMarkers,
            Is.Empty,
            "Remove files from the mutable static debt allow-list after replacing their mutable static collection fields.");
    }

    private static IEnumerable<string> FindViolations(string root, string path)
    {
        var text = File.ReadAllText(path);
        var relativePath = NormalizePath(Path.GetRelativePath(root, path));

        return MutableStaticCollectionFieldRegex
            .Matches(text)
            .Select(match => $"{relativePath}: contains mutable static collection field near '{match.Value.Trim()}'");
    }

    private static bool IsAllowedCurrentDebtFile(string root, string path)
    {
        var relativePath = NormalizePath(Path.GetRelativePath(root, path));
        return AllowedCurrentDebtFiles.Contains(relativePath, StringComparer.Ordinal);
    }

    private static bool IsGuardedProductionSourceFile(string root, string path)
    {
        var relativePath = NormalizePath(Path.GetRelativePath(root, path));

        return GuardedProductionDirectories.Any(directory => relativePath.StartsWith(directory, StringComparison.Ordinal))
               && !relativePath.Contains("/Tests/", StringComparison.Ordinal)
               && !relativePath.Contains("/bin/", StringComparison.Ordinal)
               && !relativePath.Contains("/obj/", StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null)
        {
            var marker = Path.Combine(directory.FullName, "UniversalToolchain", "Tests", "Tests.csproj");
            if (File.Exists(marker))
                return directory.FullName;

            directory = directory.Parent;
        }

        Assert.Fail("Repository root was not found from the test directory.");
        return string.Empty;
    }

    private static string NormalizePath(string path) => path.Replace(Path.DirectorySeparatorChar, '/');
}
