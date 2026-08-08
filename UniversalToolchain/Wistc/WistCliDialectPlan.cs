namespace Wistc;

internal sealed record WistCliDialectPlan(
    WistCliDialectPlanKind Kind,
    string BasePresetId,
    string? CustomizedDialectText)
{
    public static WistCliDialectPlan Preset(string basePresetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePresetId);
        return new WistCliDialectPlan(WistCliDialectPlanKind.Preset, basePresetId, null);
    }
}
