using BasicCore.Core;
using ExceptionsManager;

namespace AbstractIrConverters;

public class BytecodeToAbstractIrConverterImpl : IAbstractMethodsTranslator
{
    private readonly IInstructionIntrinsicReader _intrinsicReader;
    private readonly IIntrinsicTypeStackProcessor _processor;

    public BytecodeToAbstractIrConverterImpl(
        IInstructionIntrinsicReader intrinsicReader,
        IIntrinsicTypeStackProcessor processor)
    {
        intrinsicReader = intrinsicReader.ArgNotNull();

        processor = processor.ArgNotNull();

        _intrinsicReader = intrinsicReader;
        _processor = processor;
    }

    public IAbstractIR Translate(Bytecode bytecode)
    {
        var ir = new AbstractIR();
        var typesStack = new List<Type>();
        var unused = 0;
        foreach (var instruction in bytecode.Instructions)
        foreach (var op in instruction.Ops)
        foreach (var convertable in op.Value)
        {
            var context = new IAbstractMethodConvertable.Context(typesStack);
            var air = convertable.GetAbstractIR(context);

            ir.AppendInstructions(air.Instructions);

            InstructionTypeStackApplier.Apply(
                air.Instructions,
                typesStack,
                _intrinsicReader,
                _processor);
            unused++;
        }
        return ir;
    }
}