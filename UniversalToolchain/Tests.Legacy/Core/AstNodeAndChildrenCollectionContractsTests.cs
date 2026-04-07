namespace Tests.Core;

[TestFixture]
public class AstNodeAndChildrenCollectionContractsTests
{
    [Test]
    public void AstNode_IndexerSetter_WithNullValue_ShouldThrowNullReferenceException()
    {
        var parent = Node("Scope", "");
        parent.Children.Add(Node("Number", "1"));

        Assert.That(() => parent[0] = null!, Throws.TypeOf<NullReferenceException>());
    }

    [Test]
    public void AstNode_SafeGet_WithNegativeOrOutOfRangeIndex_ShouldReturnNull()
    {
        var parent = Node("Scope", "");
        parent.Children.Add(Node("Number", "1"));

        Assert.That(parent.SafeGet(-1), Is.Null);
        Assert.That(parent.SafeGet(1), Is.Null);
    }

    [Test]
    public void ChildrenCollection_IndexerSetter_ShouldUpdateParentOnReplacement()
    {
        var parent = Node("Scope", "");
        var initial = Node("Number", "1");
        var replacement = Node("Number", "2");
        parent.Children.Add(initial);

        parent.Children[0] = replacement;

        Assert.That(parent.Children[0], Is.SameAs(replacement));
        Assert.That(replacement.Parent, Is.SameAs(parent));
    }

    private static AstNode Node(string type, string text)
    {
        var lexeme = new LexemeValue(text, null, -1, null);
        return new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet(type), lexeme, []);
    }
}