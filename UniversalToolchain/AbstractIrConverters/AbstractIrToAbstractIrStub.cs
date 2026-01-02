using BasicCore;
using IntermediateRepresentationAbstractions;

namespace AbstractIrConverters;

public class AbstractIrToAbstractIrStub : IAbstractIrCompiler<IAbstractIR>
{
    public IReadOnlyList<string> SupportedIntrinsics =>
    [
        "call C#",
        "call C# ctor"
    ];

    public IAbstractIR Compile(IAbstractIR air) => air;
}