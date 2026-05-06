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
        "UniversalToolchain/NativeMathModule/NativeCILOptimizerModule.cs"
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
    public void NativeCilOptimizerModule_StaticGeneratorRegistry_ShouldRemainExplicitlyTrackedDebt()
    {
        var root = FindRepositoryRoot();
        var file = Path.Combine(root, "UniversalToolchain", "NativeMathModule", "NativeCILOptimizerModule.cs");
        var text = File.ReadAllText(file);

        Assert.That(
            text,
            Does.Contain("static readonly Dictionary<Type"),
            "Remove this test together with the allow-list entry after replacing the mutable static registry with an immutable mapping or data-only descriptor.");
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
