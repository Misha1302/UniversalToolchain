using AbstractIrConverters;

namespace Tests.Internal;

[TestFixture]
public class BytecodeConverterContractsTests
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
                CreateIr(new Instruction(UOpCode.Intrinsic, ["not_registered"]))))
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
