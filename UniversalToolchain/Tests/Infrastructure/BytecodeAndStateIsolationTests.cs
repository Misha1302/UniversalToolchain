using BasicCore.TranslatorWrapper;
using AbstractIrConverters;
using DynamicMethodWrapper;
using SettableGettableModule.Core;

namespace Tests.Infrastructure;

[TestFixture]
public class BytecodeAndStateIsolationTests : TestBase
{
    [Test]
    public void Should_ConvertInstructionsInOrder_When_BytecodeContainsMultipleOps()
    {
        var converter = new BytecodeToAbstractIrConverterImpl();
        var bytecode = new Bytecode([
            new BytecodeInstruction(new StubConvertable("first", _ => CreateIr(new Instruction(UOpCode.Push, [1])))),
            new BytecodeInstruction(new StubConvertable("second", _ => CreateIr(new Instruction(UOpCode.Push, [2]))))
        ]);

        var ir = converter.Translate(bytecode);

        Assert.That(ir.Instructions.Select(i => i.ToString()).ToArray(),
            Is.EqualTo(new[] { "Push 1", "Push 2" }));
    }

    [Test]
    public void Should_PassUpdatedTypeStackToNextOperation_When_ConvertingBytecode()
    {
        var converter = new BytecodeToAbstractIrConverterImpl();
        IReadOnlyList<Type>? observedStack = null;

        var bytecode = new Bytecode([
            new BytecodeInstruction(new StubConvertable("push", _ => CreateIr(new Instruction(UOpCode.Push, [1])))),
            new BytecodeInstruction(new StubConvertable("observe", ctx =>
            {
                observedStack = ctx.Stack.ToList();
                return CreateIr(new Instruction(UOpCode.Nop));
            }))
        ]);

        converter.Translate(bytecode);

        Assert.That(observedStack, Is.Not.Null);
        Assert.That(observedStack!, Is.EqualTo(new[] { typeof(int) }));
    }

    [Test]
    public void Should_Throw_When_ConvertedIntrinsicIsUnknown()
    {
        var converter = new BytecodeToAbstractIrConverterImpl();
        var bytecode = new Bytecode([
            new BytecodeInstruction(new StubConvertable("bad", _ =>
                CreateIr(new Instruction(UOpCode.Intrinsic, ["not_registered"])) ))
        ]);

        Assert.Throws<InvalidOperationException>(() => converter.Translate(bytecode));
    }

    [Test]
    public void Should_ReturnEmptyIr_When_BytecodeIsEmpty()
    {
        var converter = new BytecodeToAbstractIrConverterImpl();

        var ir = converter.Translate(new Bytecode([]));

        Assert.That(ir.Instructions, Is.Empty);
    }

    [Test]
    public void Should_IsolateValuesByKey_When_UsingVariablesContainer()
    {
        var keyA = "iso-a-" + Guid.NewGuid();
        var keyB = "iso-b-" + Guid.NewGuid();
        VariablesContainer<int>.Set(keyA, 42);
        VariablesContainer<int>.Set(keyB, 7);

        var valueA = VariablesContainer<int>.Get(keyA);
        var valueB = VariablesContainer<int>.Get(keyB);

        Assert.That(valueA, Is.EqualTo(42));
        Assert.That(valueB, Is.EqualTo(7));
    }

    [Test]
    public void Should_BeStableAcrossRepeatedRuns_When_ExecutingSameProgramMultipleTimes()
    {
        const string code = @"
            let x = 40
            let y = 2
            x + y
        ";

        var first = ExecuteCode(code);
        var second = ExecuteCode(code);

        Assert.That(second, Is.EqualTo(first));
    }


    [Test]
    public void Should_NotAccumulateBytecodeAcrossTranslateCalls_When_TranslatorIsReused()
    {
        var translator = new BasicCodeTranslator.BasicAstToBytecodeTranslatorImpl(
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

        var translator = new BasicCodeTranslator.BasicAstToBytecodeTranslatorImpl(
            new BytecodeTranslatorConfiguration([new NestedAppendingVisitor(rootTag, childTag)]));
        var ast = new AstNode(rootTag, null, [new AstNode(childTag, null, [])]);

        var translated = translator.Translate(ast);

        Assert.That(translated.Instructions.Count, Is.EqualTo(2));
    }

    [Test]
    public void Should_RegisterAirIntrinsicPredictably_When_RegisteringSameNameTwice()
    {
        var intrinsic = "test_intrinsic_" + Guid.NewGuid().ToString("N");

        var first = AirTypes.TryRegisterIntrinsic(intrinsic, (_, _) => { });
        var second = AirTypes.TryRegisterIntrinsic(intrinsic, (_, _) => { });

        Assert.That(first, Is.True);
        Assert.That(second, Is.False);
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
            data.Bytecode.Instructions.Add(new BytecodeInstruction(new StubConvertable("append", _ => CreateIr(new Instruction(UOpCode.Nop)))));
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
