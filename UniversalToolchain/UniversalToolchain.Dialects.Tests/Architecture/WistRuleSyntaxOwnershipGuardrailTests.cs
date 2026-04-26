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

        var allowedSyntaxOwners = new HashSet<string>(StringComparer.Ordinal)
        {
            Path.Combine(ruleRoot, "Syntax", "WistRuleSetSyntaxParser.cs")
        };

        var targets = Directory
            .GetFiles(ruleRoot, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(facadeRoot, "*.cs", SearchOption.AllDirectories))
            .Where(file => !allowedSyntaxOwners.Contains(file))
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
            "ConsumeArrow(",
            "TryReadKeyword("
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
            "Raw-source syntax recognition is only allowed in Wist rule syntax-owner files. Move this logic into parser-owned syntax services or consume structured parser output instead.");
    }

    [Test]
    public void RuleSyntaxOwner_MustNotParseWistBodyLanguageConstructs()
    {
        var root = ResolveRepositoryRoot();
        var syntaxOwner = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.Dialects.Wist", "Rules", "Syntax", "WistRuleSetSyntaxParser.cs");
        var content = File.ReadAllText(syntaxOwner);

        var forbiddenPatterns = new[]
        {
            "\"let\"",
            "\"if\"",
            "\"then\"",
            "\"else\"",
            "\"elif\"",
            "\"goto\""
        };

        var violations = forbiddenPatterns
            .Where(pattern => content.Contains(pattern, StringComparison.Ordinal))
            .Select(pattern => $"WistRuleSetSyntaxParser.cs contains forbidden body-level keyword pattern {pattern}.")
            .ToList();

        Assert.That(
            violations,
            Is.Empty,
            "Rule wrapper syntax parser must not recognize Wist body-language constructs.");
    }

    [Test]
    public void RuleBodySourceScanner_MustNotExist()
    {
        var root = ResolveRepositoryRoot();
        var scannerPath = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.Dialects.Wist", "Rules", "Syntax", "WistRuleBodySyntaxAnalyzer.cs");

        Assert.That(File.Exists(scannerPath), Is.False, "Raw body-source scanner must not be present in production code.");
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
