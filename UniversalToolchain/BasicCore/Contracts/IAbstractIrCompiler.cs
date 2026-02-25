using IntermediateRepresentationAbstractions;

namespace BasicCore.Contracts;

public interface IAbstractIrCompiler<out TCompilationOutput>
{
    public IReadOnlyList<string> SupportedIntrinsics =>
    [
        "call C#",
        "call C# ctor"
    ];

    public TCompilationOutput Compile(IAbstractIR air, OrderedDictionary<string, Type> parameters);
}