using IntermediateRepresentationAbstractions;

namespace BasicCore;

public interface IAbstractIrCompiler<out TCompilationOutput>
{
    public IReadOnlyList<string> SupportedIntrinsics =>
    [
        "call C#",
        "call C# ctor"
    ];

    public TCompilationOutput Compile(IAbstractIR air);
}