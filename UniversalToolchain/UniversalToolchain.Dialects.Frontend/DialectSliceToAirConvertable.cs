using DynamicMethodWrapper;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;

namespace UniversalToolchain.Dialects.Frontend;

/// <summary>
/// Converts the parsed dialect definition slice into explicit AIR annotation payload.
/// </summary>
public sealed class DialectSliceToAirConvertable(DialectDefinitionSlice slice) : IAbstractMethodConvertable
{
    public string Name => "dialect_slice_annotation";

    public IAbstractIR GetAbstractIR(IAbstractMethodConvertable.Context context)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions([new Instruction(UOpCode.Annotate, metadata: [slice], comment: "dialect definition slice")]);
        return ir;
    }
}
