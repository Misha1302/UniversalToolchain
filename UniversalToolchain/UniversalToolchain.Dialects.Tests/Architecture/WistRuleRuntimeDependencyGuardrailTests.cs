namespace UniversalToolchain.Dialects.Tests.Architecture;

public sealed class WistRuleRuntimeDependencyGuardrailTests
{
    [Test]
    public void WistRulesLayer_MustNotDependOnNumbersModuleImplementationDetails()
    {
        var root = ResolveRepositoryRoot();
        var ruleRoot = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.Dialects.Wist", "Rules");
        if (!Directory.Exists(ruleRoot))
            Assert.Pass("Wist rules directory is not present in this repository state.");

        var files = Directory.GetFiles(ruleRoot, "*.cs", SearchOption.AllDirectories)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();

        var forbiddenPatterns = new[]
        {
            "NumbersModule.Core",
            "RealNumberImpl"
        };

        var violations = new List<string>();
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            foreach (var pattern in forbiddenPatterns)
            {
                if (content.Contains(pattern, StringComparison.Ordinal))
                    violations.Add($"{Path.GetRelativePath(root, file)} contains forbidden pattern '{pattern}'.");
            }
        }

        Assert.That(violations, Is.Empty);
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "UniversalToolchain", "Wist.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root with UniversalToolchain/Wist.sln was not found.");
    }
}
