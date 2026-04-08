using ExceptionsManager;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;
using UniversalToolchain.Intrinsics.Legacy;

namespace AbstractIrConverters;

public class BytecodeToAbstractIrConverterImpl : IAbstractMethodsTranslator
{
    private readonly ILegacyIntrinsicDecoder _decoder;
    private readonly IIntrinsicTypeStackProcessor _processor;

    public BytecodeToAbstractIrConverterImpl(
        ILegacyIntrinsicDecoder decoder,
        IIntrinsicTypeStackProcessor processor)
    {
        if (decoder == null)
            Thrower.ArgumentNull(nameof(decoder));

        if (processor == null)
            Thrower.ArgumentNull(nameof(processor));

        _decoder = decoder!;
        _processor = processor!;
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
                _decoder,
                _processor);
            unused++;
        }
        return ir;
    }
}
