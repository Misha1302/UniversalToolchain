using BasicCore;
using IntermediateRepresentationAbstractions;

namespace AbstractIrConverters;

public class AbstractIrToAbstractIrStub : IAbstractIrCompiler<IAbstractIR>
{
    public IAbstractIR Compile(IAbstractIR air)
    {
        return air;
    }
}