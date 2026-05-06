using System.Text.RegularExpressions;

namespace Tests.Architecture;

[TestFixture]
public sealed class StaticStateGuardrailTests
{
    private static readonly Regex MutableStaticCollectionFieldRegex = new(
        @"\b(?:private|internal|protected|public)?\s*static\s+(?:readonly\s+)?(?:Dictionary|List|HashSet|OrderedDictionary|ConcurrentDictionary)\s*(?:<|\s+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] AllowedCurrentDebtFiles =
    [
        "UniversalToolchain/NativeMathModule/NativeCILOptimizerModule.cs",
        "UniversalToolchain/UniversalToolchain.Dialects.Parsing/DialectSyntaxDocument.cs",
        "UniversalToolchain/UniversalToolchain.Dialects.Wist/DialectIntrinsicPolicyCompiler.cs",
        "UniversalToolchain/UniversalToolchain.Dialects.Wist/WistDialectExecutionConfiguration.cs",
        "UniversalToolchain/UniversalToolchain.Dialects.Wist/WistDialectExecutionConfigurationBuilder.cs",
        "UniversalToolchain/UniversalToolchain.Dialects.Wist/WistDeclaredBindingFactory.cs",
        "UniversalToolchain/UniversalToolchain.Dialects.Wist/SelectedRuntimeExecutionShape.cs",
        "UniversalToolchain/UniversalToolchain.Dialects.Integration/RuntimeModuleDescriptor.cs",
        "UniversalToolchain/UniversalToolchain.Dialects.Integration/DialectBackendRuntimeConfiguration.cs"
    ];

    [Test]
    public void ProductionCode_ShouldNotIntroduceNewMutableStaticCollectionFields()
    {
        var root = FindRepositoryRoot();
        var universalToolchainRoot = Path.Combine(root, "UniversalToolchain");
        var files = Directory.GetFiles(universalToolchainRoot, "*.cs", SearchOption.AllDirectories)
            .Where(IsProductionSourceFile)
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
    public void KnownMutableStaticCollectionDebt_ShouldRemainExplicitlyTracked()
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

    private static bool IsProductionSourceFile(string path)
    {
        var normalized = NormalizePath(path);

        return !normalized.Contains("/Tests/", StringComparison.Ordinal)
               && !normalized.Contains("/Tests.Legacy/", StringComparison.Ordinal)
               && !normalized.Contains("/UniversalToolchain.Dialects.Tests/", StringComparison.Ordinal)
               && !normalized.Contains("/bin/", StringComparison.Ordinal)
               && !normalized.Contains("/obj/", StringComparison.Ordinal);
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
