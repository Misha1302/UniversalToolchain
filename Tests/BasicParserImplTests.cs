// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using ArithmeticModule;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicParser;
using BasicTypesExtensions;

namespace Tests;

[TestFixture]
public class BasicParserImplTests
{
    [Test]
    public void Parse_WithSimpleTokens_CreatesAST()
    {
        // Arrange
        var parser = new BasicParserImpl();
        parser.Configuration.NodeCreators.Add(0, new AdditionOperationNodeCreator());

        var lexemes = new List<LexemeValue>
        {
            new("123", new LexemePattern("\\d+", ExtensibleEnum<LexemeTag>.CreateOrGet("Number")), 0, null),
            new("+", new LexemePattern("\\+", ExtensibleEnum<LexemeTag>.CreateOrGet("Addition")), 4, null),
            new("456", new LexemePattern("\\d+", ExtensibleEnum<LexemeTag>.CreateOrGet("Number")), 6, null)
        };

        // Act
        var result = parser.Parse(lexemes);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Children, Has.Count.GreaterThan(0));
    }

    [Test]
    public void TreeValidator_WithValidTree_ReturnsTrue()
    {
        // Arrange
        var validator = new TreeValidator();
        var root = new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope"), null,
            [new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Number"), null, [])]);

        // Act
        var isValid = validator.IsValidTree(root);

        // Assert
        Assert.That(isValid, Is.True);
    }
}