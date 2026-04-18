namespace Tests.Core;

[TestFixture]
public class BasicParserProgramRootTests
{
    [Test]
    public void Parse_ShouldCreateProgramRootNode()
    {
        var parser = new BasicParserImpl();
        var lexemes = new List<LexemeValue>
        {
            new("123", new LexemePattern("\\d+", ExtensibleEnum<LexemeTag>.CreateOrGet("Number")), 0, null)
        };

        var result = parser.Parse(lexemes);

        Assert.That(result.NodeType, Is.EqualTo(ExtensibleEnum<AstNodeTag>.CreateOrGet("Program")));
    }
}
