namespace Tests.Core;

[TestFixture]
public class ParserLexerInvariantContractsTests
{
    [Test]
    public void BasicLexerImpl_Lexemize_WithUncoveredInput_ShouldThrowLexerException()
    {
        var lexer = new BasicLexerImpl();
        lexer.Configuration.AddPattern(
            new LexemePattern(@"[a-z]+", ExtensibleEnum<LexemeTag>.CreateOrGet("Word"))
        );

        var ex = Assert.Throws<LexerException>(() => lexer.Lexemize("ok ?"));

        Assert.That(ex, Is.TypeOf<LexerException>());
        Assert.That(ex!.Message, Does.Contain("Invalid token"));
    }

    [Test]
    public void BasicParserImpl_Parse_WhenTreeContainsUnknownNode_ShouldSurfaceValidatorFailure()
    {
        var parser = new BasicParserImpl();
        var lexemes = new List<LexemeValue>
        {
            new("x", null, 0, "x")
        };

        var ex = Assert.Throws<InvalidOperationException>(() => parser.Parse(lexemes));

        Assert.That(ex, Is.TypeOf<InvalidOperationException>());
        Assert.That(ex!.Message, Does.Contain("Tree is invalid"));
    }

    [Test]
    public void BasicParserImpl_Parse_ShouldNotSilentlyAcceptTreeValidatorFailure()
    {
        var parser = new BasicParserImpl();
        var lexemes = new List<LexemeValue>
        {
            new("1", null, 0, "1")
        };

        Assert.That(() => parser.Parse(lexemes), Throws.InvalidOperationException.With.Message.Contains("Tree is invalid"));
    }
}
