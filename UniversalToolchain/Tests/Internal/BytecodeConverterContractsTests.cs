using AbstractIrConverters;
using BasicCore.Builtins;
using BasicCore.Legacy;

namespace Tests.Internal;

[TestFixture]
public class BytecodeConverterContractsTests
{
    [Test]
    public void Should_ConvertInstructionsInOrder_When_BytecodeContainsMultipleOps()
    {
        var converter = CreateConverter();
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
        var converter = CreateConverter();
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
        var converter = CreateConverter();
        var bytecode = new Bytecode([
            new BytecodeInstruction(new StubConvertable("bad", _ =>
                CreateIr(new Instruction(UOpCode.Intrinsic, ["not_registered"]))))
        ]);

        var exception = Assert.Throws<InvalidOperationException>(() => converter.Translate(bytecode));

        Assert.That(exception!.Message, Does.Contain("Unable to read intrinsic invocation"));
        Assert.That(exception.Message, Does.Contain("not_registered"));
    }

    [Test]
    public void Should_ReturnEmptyIr_When_BytecodeIsEmpty()
    {
        var converter = CreateConverter();

        var ir = converter.Translate(new Bytecode([]));

        Assert.That(ir.Instructions, Is.Empty);
    }

    private static IAbstractIR CreateIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }

    private static BytecodeToAbstractIrConverterImpl CreateConverter() =>
        new(
            new InstructionIntrinsicReader(new LegacyIntrinsicDecoder()),
            CreateTypeStackProcessor());

    private static IIntrinsicTypeStackProcessor CreateTypeStackProcessor()
    {
        var catalog = new IntrinsicCatalogBuilder().Build(CreateDescriptorProviders());
        return new IntrinsicTypeStackProcessor(catalog, new IntrinsicTypeResolutionContext());
    }

    private static IIntrinsicDescriptorProvider[] CreateDescriptorProviders() =>
    [
        new ArithmeticIntrinsicDescriptorProvider(),
        new ComparisonIntrinsicDescriptorProvider(),
        new BooleanIntrinsicDescriptorProvider(),
        new StorageIntrinsicDescriptorProvider(),
        new CoreIntrinsicDescriptorProvider(new MethodCallTypeSemanticsResolver())
    ];

    private sealed class StubConvertable(string name, Func<IAbstractMethodConvertable.Context, IAbstractIR> factory)
        : IAbstractMethodConvertable
    {
        public string Name => name;

        public IAbstractIR GetAbstractIR(IAbstractMethodConvertable.Context context) => factory(context);
    }
}