namespace AbstractIrConverters;

public class AbstractIrToAbstractIrStub : IAbstractIrCompiler<IAbstractIR>
{
    public IReadOnlyList<string> SupportedIntrinsics =>
    [
        "call C#",
        "call C# ctor",
        "load_external",
        "store_external"
    ];

    public IAbstractIR Compile(IAbstractIR air, CompilationInput input) => air;
}
