using BasicCore.Compilation;
using BasicCore.Contracts;
using IntermediateRepresentationAbstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDefinitionSliceCompiler : IAbstractIrCompiler<DialectDefinitionSlice>
{
    public DialectDefinitionSlice Compile(IAbstractIR air, CompilationInput input)
    {
        if (air == null)
        {
            Thrower.ArgumentNull(nameof(air));
        }

        if (input == null)
        {
            Thrower.ArgumentNull(nameof(input));
        }

        return DialectDefinitionSliceAirReader.Read(air);
    }
}
