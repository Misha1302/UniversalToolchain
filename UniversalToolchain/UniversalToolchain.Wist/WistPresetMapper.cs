using ExceptionsManager;
using UniversalToolchain.Dialects.Wist.Presets;

namespace UniversalToolchain.Wist;

internal static class WistPresetMapper
{
    public static WistShippedDialectPreset ToShippedPreset(WistPreset preset)
    {
        return preset switch
        {
            WistPreset.RestrictedArithmetic => WistShippedDialectPresets.PricingRestricted,
            WistPreset.FullNativePreview => WistShippedDialectPresets.FullDefaultNative,
            WistPreset.SafeFormulas => WistShippedDialectPresets.PricingRestricted,
            WistPreset.BusinessRules => WistShippedDialectPresets.FullDefaultNative,
            WistPreset.FullTrusted => WistShippedDialectPresets.FullDefaultNative,
            _ => ThrowUnsupportedPreset(preset)
        };
    }

    private static WistShippedDialectPreset ThrowUnsupportedPreset(WistPreset preset)
    {
        Thrower.Argument(nameof(preset), $"Unsupported Wist preset '{preset}'.");
        return null!;
    }
}
