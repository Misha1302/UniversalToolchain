namespace Tests;

[TestFixture]
public class BasicParserImplTests
{
    [Test]
    public void Parse_WithSimpleTokens_CreatesAST()
    {
        var parser = new BasicParserImpl();
        parser.Configuration.NodeCreators.Add(0, new AdditionOperationNodeCreator());

        var lexemes = new List<LexemeValue>
        {
            new("123", new LexemePattern("\\d+", ExtensibleEnum<LexemeTag>.CreateOrGet("Number")), 0, null),
            new("+", new LexemePattern("\\+", ExtensibleEnum<LexemeTag>.CreateOrGet("Addition")), 4, null),
            new("456", new LexemePattern("\\d+", ExtensibleEnum<LexemeTag>.CreateOrGet("Number")), 6, null)
        };


        var result = parser.Parse(lexemes);


        Assert.That(result, Is.Not.Null);
        Assert.That(result.Children, Has.Count.GreaterThan(0));
    }

    [Test]
    public void TreeValidator_WithValidTree_ReturnsTrue()
    {
        var validator = new TreeValidator();
        var root = new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope"), null,
            [new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Number"), null, [])]);


        var isValid = validator.IsValidTree(root);


        Assert.That(isValid, Is.True);
    }
}