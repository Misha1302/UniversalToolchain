namespace Wistc;

internal sealed record WistCliDialectPlan(
    WistCliDialectPlanKind Kind,
    WistShippedDialectPreset BasePreset,
    string? CustomizedDialectText)
{
    public static WistCliDialectPlan Preset(WistShippedDialectPreset basePreset)
        => new(WistCliDialectPlanKind.Preset, basePreset.ArgNotNull(), null);
}