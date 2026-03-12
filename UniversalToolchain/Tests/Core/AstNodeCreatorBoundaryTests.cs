using ArithmeticModule.Creators;
using ConditionsModule.Creators;
using CSharpInteropModule.Creators;

namespace Tests.Core;

[TestFixture]
public class AstNodeCreatorBoundaryTests
{
    [Test]
    public void AdditionTryCreateNode_OperatorAtStart_DoesNotThrowAndReturnsFalse()
    {
        var creator = new AdditionOperationNodeCreator();
        var scope = CreateScope([
            Node("Addition", "+"),
            Node("Number", "1")
        ]);

        Assert.DoesNotThrow(() =>
        {
            var result = creator.TryCreateNode(scope, 0);
            Assert.That(result, Is.False, "Creator should reject malformed binary operation at scope start.");
        });
    }

    [Test]
    public void AdditionTryCreateNode_OperatorAtLastIndex_DoesNotThrowAndReturnsFalse()
    {
        var creator = new AdditionOperationNodeCreator();
        var scope = CreateScope([
            Node("Number", "1"),
            Node("Addition", "+")
        ]);

        Assert.DoesNotThrow(() =>
        {
            var result = creator.TryCreateNode(scope, 1);
            Assert.That(result, Is.False, "Creator should reject malformed binary operation at scope end.");
        });
    }

    [Test]
    public void CSharpFunctionCallsTryCreateNode_MethodAtLastIndex_DoesNotThrowAndReturnsFalse()
    {
        var creator = new CSharpFunctionCallsNodeCreator();
        var scope = CreateScope([
            Node("Identifier", "System.Console.WriteLine")
        ]);

        Assert.DoesNotThrow(() =>
        {
            var result = creator.TryCreateNode(scope, 0);
            Assert.That(result, Is.False, "Creator should reject method call when argument node is absent.");
        });
    }

    [Test]
    public void BooleanTryCreateNode_MissingRightOperand_ReturnsFalse()
    {
        var creator = new BooleanNodeCreator("And", BooleanNodeCreator.BooleanStatementType.BinaryOperation);
        var scope = CreateScope([
            Node("BooleanLiteral", "True"),
            Node("And", "And")
        ]);

        var result = creator.TryCreateNode(scope, 1);

        Assert.That(result, Is.False, "Binary boolean creator should reject node with missing right operand.");
    }

    [Test]
    public void ComparisonTryCreateNode_MissingLeftOperand_ReturnsFalse()
    {
        var creator = new ComparisonNodeCreator("Equal");
        var scope = CreateScope([
            Node("Equal", "=="),
            Node("Number", "1")
        ]);

        var result = creator.TryCreateNode(scope, 0);

        Assert.That(result, Is.False, "Comparison creator should reject node with missing left operand.");
    }

    private static AstNode CreateScope(List<AstNode> children) =>
        new(ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope"), null, children);

    private static AstNode Node(string nodeType, string text) =>
        new(ExtensibleEnum<AstNodeTag>.CreateOrGet(nodeType), new LexemeValue(text, null, -1, null), []);
}