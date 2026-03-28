namespace Tests.Core;

[TestFixture]
public class BasicLexerImplTests
{
    [Test]
    public void Lexemize_WithNumbers_ReturnsNumberTokens()
    {
        var lexer = new BasicLexerImpl();
        lexer.Configuration.AddPattern(
            new LexemePattern(@"(\+|-)?([0-9]+)(\.[0-9]+)?",
                ExtensibleEnum<LexemeTag>.CreateOrGet("Number")),
            priority: 100
        );
        lexer.Configuration.AddPattern(
            new LexemePattern(" ",
                ExtensibleEnum<LexemeTag>.CreateOrGet("Space")),
            true
        );


        var result = lexer.Lexemize("123 45.67 -89");


        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0].Text, Is.EqualTo("123"));
        Assert.That(result[1].Text, Is.EqualTo("45.67"));
        Assert.That(result[2].Text, Is.EqualTo("-89"));
    }

    [Test]
    public void Lexemize_WithIdentifiers_ReturnsIdentifierTokens()
    {
        var lexer = new BasicLexerImpl();
        lexer.Configuration.AddPattern(
            new LexemePattern("[a-zA-Z_][a-zA-Z0-9_]*",
                ExtensibleEnum<LexemeTag>.CreateOrGet("Identifier")),
            priority: 100
        );
        lexer.Configuration.AddPattern(
            new LexemePattern(" ",
                ExtensibleEnum<LexemeTag>.CreateOrGet("Space")),
            true
        );


        var result = lexer.Lexemize("variable_name test123");


        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Text, Is.EqualTo("variable_name"));
        Assert.That(result[1].Text, Is.EqualTo("test123"));
    }

    [Test]
    public void Lexemize_WithIgnoredTokens_FiltersThemOut()
    {
        var lexer = new BasicLexerImpl();
        lexer.Configuration.AddPattern(
            new LexemePattern(" ", ExtensibleEnum<LexemeTag>.CreateOrGet("Space")),
            true
        );
        lexer.Configuration.AddPattern(
            new LexemePattern("[a-zA-Z]+", ExtensibleEnum<LexemeTag>.CreateOrGet("Word")),
            priority: 100
        );


        var result = lexer.Lexemize("hello world with spaces");


        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result.All(x => x.Text != " "), Is.True);
    }


    [Test]
    public void Lexemize_WithUnknownToken_ThrowsLexerException()
    {
        var lexer = new BasicLexerImpl();
        lexer.Configuration.AddPattern(
            new LexemePattern("[a-zA-Z_][a-zA-Z0-9_]*", ExtensibleEnum<LexemeTag>.CreateOrGet("Identifier")),
            priority: 100
        );

        var exception = Assert.Throws<LexerException>(() => lexer.Lexemize("ok @ bad"));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("Invalid token"));
    }
}