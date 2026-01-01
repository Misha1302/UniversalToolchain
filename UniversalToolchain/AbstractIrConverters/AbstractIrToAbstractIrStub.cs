using BasicCore;
using IntermediateRepresentationAbstractions;

namespace AbstractIrConverters;

public class AbstractIrToAbstractIrStub : IAbstractIrCompiler<IAbstractIR>
{
    public IReadOnlyList<string> SupportedIntrinsics => [];

    public IAbstractIR Compile(IAbstractIR air)
    {
        return air;
    }
}