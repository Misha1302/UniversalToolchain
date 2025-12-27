using IntermediateRepresentationAbstractions;

namespace LocalVariablesOptimizerModule;

public static class AbstractIRExtensions
{
    public static void PushGeneric<T>(this IAbstractIR ir, T value)
    {
        ir.Push(value);
    }
}