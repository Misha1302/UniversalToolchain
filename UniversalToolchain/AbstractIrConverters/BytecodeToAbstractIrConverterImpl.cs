using AbstractIrExtensions;
using BasicCore;
using BasicCore.TranslatorWrapper;
using DotnetAirHelper;
using DynamicMethodWrapper;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;

namespace AbstractIrConverters;

public class BytecodeToAbstractIrConverterImpl : IAbstractMethodsTranslator
{
    public IAbstractIR Translate(Bytecode bytecode)
    {
        var ir = new AbstractIR();
        var typesStack = new List<Type>();
        foreach (var instruction in bytecode.Instructions)
        foreach (var op in instruction.Ops)
        foreach (var convertable in op.Value)
        {
            var context = new IAbstractMethodConvertable.Context(typesStack);
            var air = convertable.GetAbstractIR(context);

            ir.AppendInstructions(air);

            air.Instructions.ManipulateTypesStack(typesStack, AirTypes.ProcessTypesIntrinsic);
        }
        return ir;
    }
}