using AbstractIrExtensions;
using BasicCodeTranslator;

namespace Tests.Internal;

[TestFixture]
public class TranslatorStateIsolationTests
{
    [Test]
    public void Should_NotAccumulateBytecodeAcrossTranslateCalls_When_TranslatorIsReused()
    {
        var translator = new BasicAstToBytecodeTranslatorImpl(
            new BytecodeTranslatorConfiguration([new AppendingVisitor()]));
        var ast = new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Root"), null, []);

        var first = translator.Translate(ast);
        var second = translator.Translate(ast);

        Assert.That(first.Instructions.Count, Is.EqualTo(1));
        Assert.That(second.Instructions.Count, Is.EqualTo(1));
    }

    [Test]
    public void Should_AppendNestedTranslationIntoSameRequestBytecode()
    {
        var rootTag = ExtensibleEnum<AstNodeTag>.CreateOrGet("NestedRoot");
        var childTag = ExtensibleEnum<AstNodeTag>.CreateOrGet("NestedChild");

        var translator = new BasicAstToBytecodeTranslatorImpl(
            new BytecodeTranslatorConfiguration([new NestedAppendingVisitor(rootTag, childTag)]));
        var ast = new AstNode(rootTag, null, [new AstNode(childTag, null, [])]);

        var translated = translator.Translate(ast);

        Assert.That(translated.Instructions.Count, Is.EqualTo(2));
    }

    [Test]
    public void BuiltInIntrinsicSequence_ShouldRemainTypeStackValid_ForDoubleArithmeticExpression()
    {
        var ir = CreateIr(
            new Instruction(UOpCode.Intrinsic, ["load_external", 0, typeof(double)]),
            new Instruction(UOpCode.Intrinsic, ["load_f64", 0.9d]),
            new Instruction(UOpCode.Intrinsic, ["mul_f64"]),
            new Instruction(UOpCode.Intrinsic, ["load_external", 1, typeof(double)]),
            new Instruction(UOpCode.Intrinsic, ["add_f64"]));

        var stack = new List<Type>();

        Assert.DoesNotThrow(() => ir.Instructions.ManipulateTypesStack(stack, IntrinsicTypeProcessor.ProcessTypes));
        Assert.That(stack, Has.Count.EqualTo(1));
        Assert.That(stack[0], Is.EqualTo(typeof(double)));
    }

    private sealed class NestedAppendingVisitor(
        ExtensibleEnum<AstNodeTag> rootTag,
        ExtensibleEnum<AstNodeTag> childTag) : IAstVisitor
    {
        public void TryVisit(BytecodeVisitorData data)
        {
            if (data.Node.NodeType == rootTag)
                data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);

            if (data.Node.NodeType == rootTag || data.Node.NodeType == childTag)
                data.Bytecode.Instructions.Add(new BytecodeInstruction(
                    new StubConvertable("nested", _ => CreateIr(new Instruction(UOpCode.Nop)))));
        }
    }

    private sealed class AppendingVisitor : IAstVisitor
    {
        public void TryVisit(BytecodeVisitorData data)
        {
            data.Bytecode.Instructions.Add(new BytecodeInstruction(
                new StubConvertable("append", _ => CreateIr(new Instruction(UOpCode.Nop)))));
        }
    }

    private static IAbstractIR CreateIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }

    private sealed class StubConvertable(string name, Func<IAbstractMethodConvertable.Context, IAbstractIR> factory)
        : IAbstractMethodConvertable
    {
        public string Name => name;

        public IAbstractIR GetAbstractIR(IAbstractMethodConvertable.Context context) => factory(context);
    }
}