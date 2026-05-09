using ExceptionsManager;
using UniversalToolchain.Dialects.Wist.Presets;

namespace UniversalToolchain.Wist;

internal static class WistPresetMapper
{
    public static WistShippedDialectPreset ToShippedPreset(WistPreset preset)
    {
        return preset switch
        {
            WistPreset.SafeFormulas => WistShippedDialectPresets.PricingRestricted,
            WistPreset.BusinessRules => WistShippedDialectPresets.FullDefaultNative, // Alias of FullTrusted in preview.
            WistPreset.FullTrusted => WistShippedDialectPresets.FullDefaultNative,
            _ => Thrower.Argument(nameof(preset), "Unsupported Wist preset.")
        };
    }
}