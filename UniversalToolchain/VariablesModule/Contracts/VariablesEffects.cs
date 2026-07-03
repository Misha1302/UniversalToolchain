using UniversalToolchain.ModuleContracts;

namespace VariablesModule.Contracts;

public static class VariablesEffects
{
    public static CompilerEffectId LowerVariableAccess { get; } = new("wist.variables.effect.lower-variable-access");
}
