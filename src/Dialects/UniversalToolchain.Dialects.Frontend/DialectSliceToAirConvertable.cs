using DynamicMethodWrapper;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectSliceToAirConvertable(IReadOnlyList<object> annotations) : IAbstractMethodConvertable
{
    public string Name => "dialect_annotations";

    public IAbstractIR GetAbstractIR(IAbstractMethodConvertable.Context context)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions([new Instruction(UOpCode.Annotate, metadata: annotations.ToList(), comment: "dialect definition annotations")]);
        return ir;
    }
}