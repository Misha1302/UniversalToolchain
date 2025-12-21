using BasicCore.LexerWrapper;
using BasicLexer;
using BasicTypesExtensions;

namespace Tests;

[TestFixture]
public class BasicLexerImplTests
{
    [Test]
    public void Lexemize_WithNumbers_ReturnsNumberTokens()
    {
        // Arrange
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

        // Act
        var result = lexer.Lexemize("123 45.67 -89");

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0].Text, Is.EqualTo("123"));
        Assert.That(result[1].Text, Is.EqualTo("45.67"));
        Assert.That(result[2].Text, Is.EqualTo("-89"));
    }

    [Test]
    public void Lexemize_WithIdentifiers_ReturnsIdentifierTokens()
    {
        // Arrange
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

        // Act
        var result = lexer.Lexemize("variable_name test123");

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Text, Is.EqualTo("variable_name"));
        Assert.That(result[1].Text, Is.EqualTo("test123"));
    }

    [Test]
    public void Lexemize_WithIgnoredTokens_FiltersThemOut()
    {
        // Arrange
        var lexer = new BasicLexerImpl();
        lexer.Configuration.AddPattern(
            new LexemePattern(" ", ExtensibleEnum<LexemeTag>.CreateOrGet("Space")),
            true
        );
        lexer.Configuration.AddPattern(
            new LexemePattern("[a-zA-Z]+", ExtensibleEnum<LexemeTag>.CreateOrGet("Word")),
            priority: 100
        );

        // Act
        var result = lexer.Lexemize("hello world with spaces");

        // Assert
        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result.All(x => x.Text != " "), Is.True);
    }
}