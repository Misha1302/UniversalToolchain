using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Capabilities;

public static class OptimizerCapabilityGuards
{
    public static bool SupportsAll(
        IOptimizerIntrinsicCapabilityContext capabilityContext,
        IEnumerable<(IntrinsicSymbol Symbol, Type[] TypeArguments)> requirements)
    {
        if (capabilityContext == null)
            Thrower.ArgumentNull(nameof(capabilityContext));

        if (requirements == null)
            Thrower.ArgumentNull(nameof(requirements));

        foreach (var requirement in requirements)
        {
            if (!capabilityContext.Supports(requirement.Symbol, requirement.TypeArguments))
                return false;
        }

        return true;
    }

    public static bool SupportsAll(
        IOptimizerIntrinsicCapabilityContext capabilityContext,
        params (IntrinsicSymbol Symbol, Type[] TypeArguments)[] requirements)
        => SupportsAll(capabilityContext, (IEnumerable<(IntrinsicSymbol Symbol, Type[] TypeArguments)>)requirements);
}
