namespace Wistc;

internal sealed record WistCliDialectPlan(
    WistCliDialectPlanKind Kind,
    WistShippedDialectPreset BasePreset,
    string? CustomizedDialectText)
{
    public static WistCliDialectPlan Preset(WistShippedDialectPreset basePreset)
        => new(WistCliDialectPlanKind.Preset, basePreset.ArgNotNull(), null);

    public static WistCliDialectPlan CustomizedPreset(WistShippedDialectPreset basePreset, string customizedDialectText)
    {
        basePreset = basePreset.ArgNotNull();
        if (string.IsNullOrWhiteSpace(customizedDialectText))
            Thrower.Argument(nameof(customizedDialectText), "Customized dialect text must not be empty.");

        return new WistCliDialectPlan(WistCliDialectPlanKind.CustomizedPreset, basePreset, customizedDialectText);
    }
}
