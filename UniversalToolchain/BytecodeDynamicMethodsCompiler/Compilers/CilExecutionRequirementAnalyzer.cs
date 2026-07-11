using BasicCore.Core;

namespace BytecodeDynamicMethodsCompiler.Compilers;

internal static class CilExecutionRequirementAnalyzer
{
    public static CilExecutionRequirements Analyze(IAbstractIR air)
    {
        air = air.ArgNotNull();

        foreach (var instruction in air.Instructions)
        {
            if (RequiresExecutionEnvironment(instruction))
                return new CilExecutionRequirements(true);
        }

        return new CilExecutionRequirements(false);
    }

    private static bool RequiresExecutionEnvironment(Instruction instruction)
    {
        return CSharpCallIntrinsicReader.TryGetCallDescriptor(instruction, out var descriptor)
               && descriptor.Receiver is CSharpCallReceiver.ExecutionScopedProvider;
    }
}

internal readonly record struct CilExecutionRequirements(bool RequiresExecutionEnvironment);