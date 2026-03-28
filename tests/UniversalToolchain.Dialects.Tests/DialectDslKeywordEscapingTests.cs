using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Tests;

public class DialectDslKeywordEscapingTests
{
    [TestCase("meta+", "meta+ value", "meta value")]
    [TestCase("meta?", "meta? value", "meta value")]
    [TestCase("meta.", "meta. value", "meta.a value")]
    [TestCase("meta[", "meta[ value", "meta value")]
    [TestCase("meta|pipe", "meta|pipe value", "meta value")]
    public void KeywordLexemeRegistration_ShouldEscapeRegexMetacharacters(string keyword, string matchingDirectiveLine, string similarNonMatchLine)
    {
        var registry = new DialectDslRegistry([new RegexKeywordDirectiveFeature(keyword, 100)], []);

        var matchingLexemes = DialectDslTestSupport.Lex(registry, $"dialect Demo\n{matchingDirectiveLine}\n");
        var nonMatchingLexemes = DialectDslTestSupport.Lex(registry, $"dialect Demo\n{similarNonMatchLine}\n");

        Assert.Multiple(() =>
        {
            Assert.That(matchingLexemes.Any(x => x.Text == keyword && x.LexemePattern!.LexemeType.GetName() == $"DialectDirectiveKeyword.{keyword}"), Is.True);
            Assert.That(nonMatchingLexemes.Any(x => x.LexemePattern!.LexemeType.GetName() == $"DialectDirectiveKeyword.{keyword}"), Is.False);
        });
    }

    [TestCase("meta+", 201)]
    [TestCase("meta?", 202)]
    [TestCase("meta.", 203)]
    [TestCase("meta[", 204)]
    [TestCase("meta|pipe", 205)]
    public void EscapedRegexKeywords_ShouldParseAndLower_AsLiteralDirectiveKeywords(string keyword, int sequence)
    {
        var compiler = CreateCompiler(keyword, sequence);

        var slice = compiler.Compile($"dialect Demo\n{keyword} token\n");

        Assert.That(slice.CapabilityDirectives.Select(x => x.Name), Is.EqualTo(new[] { $"{keyword}:token" }));
    }

    [Test]
    public void KeywordPattern_ShouldRequireLiteralBoundary_AroundEscapedKeyword()
    {
        var registry = new DialectDslRegistry([new RegexKeywordDirectiveFeature("meta.", 300)], []);

        var lexemes = DialectDslTestSupport.Lex(registry, "dialect Demo\nmeta.a value\n");

        Assert.That(lexemes.Any(x => x.Text == "meta."), Is.False);
    }

    private static DialectDslCompiler CreateCompiler(string keyword, int sequence)
    {
        var registry = new DialectDslRegistry([new RegexKeywordDirectiveFeature(keyword, sequence)], []);
        return new DialectDslCompiler(new DialectDslFrontendModule(registry));
    }
}