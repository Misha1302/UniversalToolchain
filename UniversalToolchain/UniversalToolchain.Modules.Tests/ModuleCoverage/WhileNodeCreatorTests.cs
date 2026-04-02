using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicTypesExtensions;
using LoopsModule.Creators;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class WhileNodeCreatorTests
{
    [Test]
    public void ConditionAndBodyPresent_ReturnsTrueAndAttachesBothChildren()
    {
        var creator = new WhileNodeCreator();
        var scope = CreateScope([
            Node("While", "while"),
            Node("Condition", "cond"),
            Node("Body", "body")
        ]);

        var result = creator.TryCreateNode(scope, 0);

        Assert.That(result, Is.True);
        Assert.That(scope.Children, Has.Count.EqualTo(1));
        Assert.That(scope[0].Children, Has.Count.EqualTo(2));
        Assert.That(scope[0].Children[0].Text, Is.EqualTo("cond"));
        Assert.That(scope[0].Children[1].Text, Is.EqualTo("body"));
    }

    [Test]
    public void MissingCondition_ReturnsFalse()
    {
        var creator = new WhileNodeCreator();
        var scope = CreateScope([
            Node("While", "while")
        ]);

        var result = creator.TryCreateNode(scope, 0);

        Assert.That(result, Is.False);
    }

    [Test]
    public void MissingBody_ReturnsFalse()
    {
        var creator = new WhileNodeCreator();
        var scope = CreateScope([
            Node("While", "while"),
            Node("Condition", "cond")
        ]);

        var result = creator.TryCreateNode(scope, 0);

        Assert.That(result, Is.False);
    }

    [Test]
    public void NonWhileNode_ReturnsFalse()
    {
        var creator = new WhileNodeCreator();
        var scope = CreateScope([
            Node("Identifier", "x"),
            Node("Condition", "cond"),
            Node("Body", "body")
        ]);

        var result = creator.TryCreateNode(scope, 0);

        Assert.That(result, Is.False);
    }

    private static AstNode CreateScope(List<AstNode> children) =>
        new(ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope"), null, children);

    private static AstNode Node(string nodeType, string text) =>
        new(ExtensibleEnum<AstNodeTag>.CreateOrGet(nodeType), new LexemeValue(text, null, -1, null), []);
}
