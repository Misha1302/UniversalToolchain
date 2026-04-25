using BasicCore.ParserWrapper;
using FunctionCallsModule;

namespace UniversalToolchain.Dialects.Tests.Capabilities;

public sealed class FunctionCallsNodeCreatorTests
{
    [Test]
    public void TryCreateNode_WhenIdentifierIsFollowedByScope_CreatesGenericFunctionCall()
    {
        var root = CreateRoot(CreateIdentifier("customFunction"), CreateScope(CreateIdentifier("value")));
        var creator = new FunctionCallsNodeCreator();

        var created = creator.TryCreateNode(root, 0);

        Assert.Multiple(() =>
        {
            Assert.That(created, Is.True);
            Assert.That(root.Children, Has.Count.EqualTo(1));
            Assert.That(root.Children[0].NodeType, Is.EqualTo(AstNodeType.CreateOrGet("FunctionCall")));
            Assert.That(root.Children[0].Text, Is.EqualTo("customFunction"));
            Assert.That(root.Children[0].Children, Has.Count.EqualTo(1));
            Assert.That(root.Children[0].Children[0].NodeType, Is.EqualTo(AstNodeType.CreateOrGet("Scope")));
        });
    }

    [Test]
    public void TryCreateNode_WhenIdentifierIsNotFollowedByScope_DoesNotCreateFunctionCall()
    {
        var root = CreateRoot(CreateIdentifier("value"), CreateIdentifier("other"));
        var creator = new FunctionCallsNodeCreator();

        var created = creator.TryCreateNode(root, 0);

        Assert.Multiple(() =>
        {
            Assert.That(created, Is.False);
            Assert.That(root.Children, Has.Count.EqualTo(2));
            Assert.That(root.Children[0].NodeType, Is.EqualTo(AstNodeType.CreateOrGet("Identifier")));
        });
    }

    private static AstNode CreateRoot(params AstNode[] children)
    {
        return new AstNode(AstNodeType.CreateOrGet("Program"), null, children.ToList());
    }

    private static AstNode CreateScope(params AstNode[] children)
    {
        return new AstNode(AstNodeType.CreateOrGet("Scope"), null, children.ToList());
    }

    private static AstNode CreateIdentifier(string text)
    {
        return new AstNode(
            AstNodeType.CreateOrGet("Identifier"),
            new LexemeValue(text, null, -1, null),
            []);
    }
}
