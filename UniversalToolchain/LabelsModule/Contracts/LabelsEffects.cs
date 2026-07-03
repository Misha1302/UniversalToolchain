using UniversalToolchain.ModuleContracts;

namespace LabelsModule.Contracts;

public static class LabelsEffects
{
    public static CompilerEffectId LowerLabelControlFlow { get; } = new("wist.labels.effect.lower-label-control-flow");
}
