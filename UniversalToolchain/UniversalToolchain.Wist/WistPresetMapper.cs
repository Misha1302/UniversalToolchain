using UniversalToolchain.Dialects.Wist.Presets;

namespace UniversalToolchain.Wist;

internal static class WistPresetMapper
{
    public static WistShippedDialectPreset ToShippedPreset(WistPreset preset)
    {
        return preset switch
        {
            WistPreset.SafeFormulas => WistShippedDialectPresets.PricingRestricted,
            WistPreset.BusinessRules => WistShippedDialectPresets.FullDefaultNative,
            WistPreset.FullTrusted => WistShippedDialectPresets.FullDefaultNative,
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unsupported Wist preset.")
        };
    }
}