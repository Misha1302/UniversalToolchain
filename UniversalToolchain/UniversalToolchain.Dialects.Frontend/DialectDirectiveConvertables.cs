using UniversalIntermediateRepresentation;
using DynamicMethodWrapper;
using IntermediateRepresentationAbstractions;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDirectiveConvertable(IDialectAirAnnotation annotation, string name) : IAbstractMethodConvertable
{
    public string Name { get; } = name;

    public IAbstractIR GetAbstractIR(IAbstractMethodConvertable.Context context)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions([new Instruction(UOpCode.Annotate, metadata: [annotation])]);
        return ir;
    }
}
