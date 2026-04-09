using UniversalToolchain.Intrinsics.Legacy;

namespace DotnetAirHelper;

public static class AirTypes
{
    private static Dictionary<string, Action<Instruction, List<Type>>> _customIntrinsicProcessors = [];

    public static bool TryRegisterIntrinsic(string name, Action<Instruction, List<Type>> processIntrinsic)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Intrinsic name cannot be empty.");

        if (processIntrinsic == null)
            Thrower.ArgumentNull(nameof(processIntrinsic));

        return _customIntrinsicProcessors.TryAdd(name, processIntrinsic);
    }

    public static void ProcessTypesIntrinsic(Instruction instruction, List<Type> stack)
    {
        if (instruction == null)
            Thrower.ArgumentNull(nameof(instruction));

        if (stack == null)
            Thrower.ArgumentNull(nameof(stack));

        if (instruction.Operands.Count == 0)
        {
            LegacyIntrinsicTypeProcessor.ProcessTypes(instruction, stack);
            return;
        }

        if (instruction.Operands[0] is string intrinsicName &&
            _customIntrinsicProcessors.TryGetValue(intrinsicName, out var processor))
        {
            processor(instruction, stack);
            return;
        }

        LegacyIntrinsicTypeProcessor.ProcessTypes(instruction, stack);
    }

    internal static void ResetToDefaultsForTests()
    {
        _customIntrinsicProcessors = [];
    }
}
