using IntermediateRepresentationAbstractions;

namespace LocalVariablesOptimizerModule;

// ReSharper disable once InconsistentNaming
public static class AbstractIRExtensions
{
    public static void PushGeneric<T>(this IAbstractIR ir, T value)
    {
        ir.Push(value);
    }
}