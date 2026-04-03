using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicTypesExtensions;
using ConditionsModule.Creators;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class BooleanNodeCreatorTests
{
    [Test]
    public void ConstantPath_WithMatchingNode_ReturnsTrueWithoutRestructuring()
    {
        var creator = new BooleanNodeCreator("BooleanLiteral", BooleanNodeCreator.BooleanStatementType.Constant);
        var scope = CreateScope([
            Node("BooleanLiteral", "True")
        ]);

        var result = creator.TryCreateNode(scope, 0);

        Assert.That(result, Is.True);
        Assert.That(scope.Children, Has.Count.EqualTo(1));
        Assert.That(scope[0].Children, Is.Empty);
    }

    [Test]
    public void UnaryOperation_WithOperand_AttachesOperandAndReturnsTrue()
    {
        var creator = new BooleanNodeCreator("Not", BooleanNodeCreator.BooleanStatementType.UnaryOperation);
        var scope = CreateScope([
            Node("Not", "Not"),
            Node("BooleanLiteral", "False")
        ]);

        var result = creator.TryCreateNode(scope, 0);

        Assert.That(result, Is.True);
        Assert.That(scope.Children, Has.Count.EqualTo(1));
        Assert.That(scope[0].Children, Has.Count.EqualTo(1));
        Assert.That(scope[0].Children[0].Text, Is.EqualTo("False"));
    }

    [Test]
    public void UnaryOperation_WithoutOperand_ReturnsFalse()
    {
        var creator = new BooleanNodeCreator("Not", BooleanNodeCreator.BooleanStatementType.UnaryOperation);
        var scope = CreateScope([
            Node("Not", "Not")
        ]);

        var result = creator.TryCreateNode(scope, 0);

        Assert.That(result, Is.False);
        Assert.That(scope.Children, Has.Count.EqualTo(1));
        Assert.That(scope[0].Children, Is.Empty);
    }

    [Test]
    public void BinaryOperation_WithBothOperands_AttachesBothAndReturnsTrue()
    {
        var creator = new BooleanNodeCreator("And", BooleanNodeCreator.BooleanStatementType.BinaryOperation);
        var scope = CreateScope([
            Node("BooleanLiteral", "True"),
            Node("And", "And"),
            Node("BooleanLiteral", "False")
        ]);

        var result = creator.TryCreateNode(scope, 1);

        Assert.That(result, Is.True);
        Assert.That(scope.Children, Has.Count.EqualTo(1));
        Assert.That(scope[0].Text, Is.EqualTo("And"));
        Assert.That(scope[0].Children, Has.Count.EqualTo(2));
        Assert.That(scope[0].Children[0].Text, Is.EqualTo("True"));
        Assert.That(scope[0].Children[1].Text, Is.EqualTo("False"));
    }

    [Test]
    [TestCase(0, "Missing left operand must be rejected.")]
    [TestCase(1, "Missing right operand must be rejected.")]
    public void BinaryOperation_WithMissingOperand_ReturnsFalse(int operatorIndex, string reason)
    {
        var creator = new BooleanNodeCreator("And", BooleanNodeCreator.BooleanStatementType.BinaryOperation);
        var scope = operatorIndex == 0
            ? CreateScope([
                Node("And", "And"),
                Node("BooleanLiteral", "True")
            ])
            : CreateScope([
                Node("BooleanLiteral", "True"),
                Node("And", "And")
            ]);

        var result = creator.TryCreateNode(scope, operatorIndex);

        Assert.That(result, Is.False, reason);
    }

    private static AstNode CreateScope(List<AstNode> children) =>
        new(ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope"), null, children);

    private static AstNode Node(string nodeType, string text) =>
        new(ExtensibleEnum<AstNodeTag>.CreateOrGet(nodeType), new LexemeValue(text, null, -1, null), []);
}