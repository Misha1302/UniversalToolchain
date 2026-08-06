using System.Text;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Frontend.Composition;

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
        var matchingCrLfSource = $"dialect Demo\r\n{matchingDirectiveLine}\r\n";
        var matchingCrSource = $"dialect Demo\r{matchingDirectiveLine}\r";
        var nonMatchingSource = $"dialect Demo\r\n{similarNonMatchLine}\r\n";

        var matchingCrLfLexemes = DialectDslTestSupport.Lex(registry, matchingCrLfSource);
        var matchingCrLexemes = DialectDslTestSupport.Lex(registry, matchingCrSource);
        var nonMatchingLexemes = DialectDslTestSupport.Lex(registry, nonMatchingSource);
        var matchingCrLfKeyword = matchingCrLfLexemes.Single(x =>
            x.Text == keyword && x.LexemePattern!.LexemeType.GetName() == $"DialectDirectiveKeyword.{keyword}");
        var matchingCrKeyword = matchingCrLexemes.Single(x =>
            x.Text == keyword && x.LexemePattern!.LexemeType.GetName() == $"DialectDirectiveKeyword.{keyword}");

        Assert.Multiple(() =>
        {
            Assert.That(matchingCrLfKeyword.StartIndex, Is.EqualTo(matchingCrLfSource.IndexOf(keyword, StringComparison.Ordinal)));
            Assert.That(matchingCrLfKeyword.LineNumber, Is.EqualTo(2));
            Assert.That(matchingCrLfKeyword.CharNumber, Is.Zero);
            Assert.That(matchingCrLfSource.Substring(matchingCrLfKeyword.StartIndex, matchingCrLfKeyword.Text.Length), Is.EqualTo(keyword));
            Assert.That(matchingCrKeyword.StartIndex, Is.EqualTo(matchingCrSource.IndexOf(keyword, StringComparison.Ordinal)));
            Assert.That(matchingCrKeyword.LineNumber, Is.EqualTo(2));
            Assert.That(matchingCrKeyword.CharNumber, Is.Zero);
            Assert.That(matchingCrSource.Substring(matchingCrKeyword.StartIndex, matchingCrKeyword.Text.Length), Is.EqualTo(keyword));
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
        foreach (var lineEnding in new[] { "\n", "\r\n", "\r" })
        {
            using var compiler = CreateCompiler(keyword, sequence);
            var slice = compiler.Compile($"dialect Demo{lineEnding}{keyword} token{lineEnding}");
            var lineEndingBytes = Convert.ToHexString(Encoding.UTF8.GetBytes(lineEnding));

            Assert.That(
                slice.CapabilityDirectives.Select(x => x.Name),
                Is.EqualTo(new[] { $"{keyword}:token" }),
                $"line ending UTF-8 bytes: {lineEndingBytes}");
        }
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