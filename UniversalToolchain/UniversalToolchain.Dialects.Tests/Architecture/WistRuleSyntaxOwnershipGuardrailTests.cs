namespace UniversalToolchain.Dialects.Tests.Architecture;

public sealed class WistRuleSyntaxOwnershipGuardrailTests
{
    [Test]
    public void ProductionRuleAndFacadeCode_MustNotUseRawSourceSyntaxRecognition()
    {
        var root = ResolveRepositoryRoot();
        var wistRoot = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.Dialects.Wist");
        var ruleRoot = Path.Combine(wistRoot, "Rules");
        var facadeRoot = Path.Combine(wistRoot, "Facade");

        var targets = Directory
            .GetFiles(ruleRoot, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(facadeRoot, "*.cs", SearchOption.AllDirectories))
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToList();

        var forbiddenPatterns = new[]
        {
            "System.Text.RegularExpressions",
            "Regex",
            ".Split('\\n')",
            ".Split(\"\\n\")",
            ".IndexOf(\"rule\"",
            ".IndexOf(\"let\"",
            ".Contains(\"rule\"",
            ".Contains(\"let\"",
            ".StartsWith(\"rule\"",
            ".StartsWith(\"let\"",
            "TryReadKeyword(",
            "ReadUntilMatching(",
            "SkipWhiteSpace(",
            "ReadIdentifier(",
            "ConsumeArrow(",
            "cursor",
            "scanner"
        };

        var violations = new List<string>();
        foreach (var file in targets)
        {
            var content = File.ReadAllText(file);
            foreach (var pattern in forbiddenPatterns)
            {
                if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    violations.Add($"{Path.GetRelativePath(root, file)} contains forbidden pattern '{pattern}'.");
            }
        }

        Assert.That(
            violations,
            Is.Empty,
            "Rules/facade production code must consume parser-owned syntax structures instead of rediscovering language syntax from raw source text.");
    }

    [Test]
    public void TemporaryRuleSyntaxParser_MustNotExistInProductionCode()
    {
        var root = ResolveRepositoryRoot();
        var parserPath = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.Dialects.Wist", "Rules", "Syntax", "WistRuleSetSyntaxParser.cs");
        var modelsPath = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.Dialects.Wist", "Rules", "Syntax", "WistRuleSetSyntaxModels.cs");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(parserPath), Is.False, "Temporary raw-source rule parser must not exist in production code.");
            Assert.That(File.Exists(modelsPath), Is.False, "Temporary raw-source rule syntax models must not exist in production code.");
        });
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENTS.md")))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root with AGENTS.md was not found.");
    }
}
