using BasicCore.Compilation;
using BasicCore.Contracts;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDefinitionSliceCompiler : IAbstractIrCompiler<DialectDefinitionSlice>
{
    public DialectDefinitionSlice Compile(IAbstractIR air, CompilationInput input)
    {
        air = air.ArgNotNull();

        input = input.ArgNotNull();

        return DialectDefinitionSliceAirReader.Read(air);
    }
}