namespace Wistc;

internal sealed class WistCliDialectPlanBuilder
{
    private readonly WistCliCustomizedDialectBuilder _customizedDialectBuilder = new();

    public WistCliDialectPlan Build(CommonOptions options)
    {
        options = options.ArgNotNull();

        var request = WistCliCustomizationRequest.FromOptions(options);
        if (!request.HasCustomization)
            return WistCliDialectPlan.Preset(WistShippedDialectPresets.Default);

        var basePreset = request.UseNativeMath
            ? WistShippedDialectPresets.FullDefaultNative
            : WistShippedDialectPresets.Default;

        var customizedDialectText = _customizedDialectBuilder.BuildFromPreset(basePreset, request);
        return WistCliDialectPlan.CustomizedPreset(basePreset, customizedDialectText);
    }
}
