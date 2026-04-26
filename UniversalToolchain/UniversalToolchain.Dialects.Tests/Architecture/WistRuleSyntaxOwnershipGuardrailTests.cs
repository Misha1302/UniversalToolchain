namespace UniversalToolchain.Dialects.Tests.Architecture;

public sealed class WistRuleSyntaxOwnershipGuardrailTests
{
    [Test]
    public void ProductionRuleAndFacadeCode_MustNotUseRawSourceSyntaxRecognitionOutsideSyntaxOwner()
    {
        var root = ResolveRepositoryRoot();
        var wistRoot = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.Dialects.Wist");
        var ruleRoot = Path.Combine(wistRoot, "Rules");
        var facadeRoot = Path.Combine(wistRoot, "Facade");
        var syntaxRoot = Path.Combine(ruleRoot, "Syntax");

        var targets = Directory
            .GetFiles(ruleRoot, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(facadeRoot, "*.cs", SearchOption.AllDirectories))
            .Where(file => !file.StartsWith(syntaxRoot, StringComparison.Ordinal))
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
            "ReadUntilMatching(",
            "SkipWhiteSpace(",
            "ReadIdentifier(",
            "ConsumeArrow("
        };

        var violations = new List<string>();
        foreach (var file in targets)
        {
            var content = File.ReadAllText(file);
            foreach (var pattern in forbiddenPatterns)
            {
                if (content.Contains(pattern, StringComparison.Ordinal))
                    violations.Add($"{Path.GetRelativePath(root, file)} contains forbidden pattern '{pattern}'.");
            }
        }

        Assert.That(
            violations,
            Is.Empty,
            "Raw-source syntax recognition is only allowed in Wist rule syntax-owner files. Move this logic into Rules/Syntax or consume structured parser output instead.");
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
