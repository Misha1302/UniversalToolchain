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
        if (instruction.UOpCode != UOpCode.Intrinsic)
            return false;

        var normalizedInstruction = IntrinsicInstructionNormalizer.NormalizeOrThrow(instruction);
        var intrinsicName = normalizedInstruction.Operands[0].Get<string>();

        if (intrinsicName != "call C#")
            return false;

        var operand = normalizedInstruction.Operands[1];
        var descriptor = operand as CSharpCallDescriptor;

        return descriptor?.Receiver is CSharpCallReceiver.ExecutionScopedProvider;
    }
}

internal readonly record struct CilExecutionRequirements(bool RequiresExecutionEnvironment);