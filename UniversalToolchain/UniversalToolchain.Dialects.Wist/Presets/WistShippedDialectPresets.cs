using System.Diagnostics.CodeAnalysis;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist.Presets;

/// <summary>
///     Provides the optional catalog of Wist dialect files shipped with this repository.
/// </summary>
public static class WistShippedDialectPresets
{
    /// <summary>
    ///     Gets the default shipped Wist dialect preset.
    /// </summary>
    public static WistShippedDialectPreset Default => FullDefault;

    /// <summary>
    ///     Gets the full default Wist dialect preset.
    /// </summary>
    public static WistShippedDialectPreset FullDefault { get; } = Create(
        "full-default",
        "Full default",
        "General-purpose Wist dialect with compiler and interpreter backends.");

    /// <summary>
    ///     Gets the full default Wist dialect preset with native runtime features.
    /// </summary>
    public static WistShippedDialectPreset FullDefaultNative { get; } = Create(
        "full-default-native",
        "Full default native",
        "General-purpose Wist dialect with native runtime features.");

    /// <summary>
    ///     Gets the minimal arithmetic Wist dialect preset.
    /// </summary>
    public static WistShippedDialectPreset MinimalArithmetic { get; } = Create(
        "minimal-arithmetic",
        "Minimal arithmetic",
        "Small arithmetic-only Wist dialect.");

    /// <summary>
    ///     Gets the minimal arithmetic Wist dialect preset with native runtime features.
    /// </summary>
    public static WistShippedDialectPreset MinimalArithmeticNative { get; } = Create(
        "minimal-arithmetic-native",
        "Minimal arithmetic native",
        "Small arithmetic-only Wist dialect with native runtime features.");

    /// <summary>
    ///     Gets the restricted pricing Wist dialect preset.
    /// </summary>
    public static WistShippedDialectPreset PricingRestricted { get; } = Create(
        "pricing-restricted",
        "Pricing restricted",
        "Restricted Wist dialect for pricing formulas.");


    /// <summary>
    /// Gets the experimental restricted arithmetic preset with the verifier-gated SSA route enabled.
    /// </summary>
    public static WistShippedDialectPreset Ssa { get; } = Create(
        "ssa",
        "SSA",
        "Restricted arithmetic Wist dialect with verifier-gated SSA optimization enabled.");

    /// <summary>
    ///     Gets the restricted sandbox Wist dialect preset.
    /// </summary>
    public static WistShippedDialectPreset CompositionRestricted { get; } = Create(
        "composition-restricted",
        "Composition restricted",
        "Composition-constrained Wist dialect; not a process-isolation boundary.");

    /// <summary>
    ///     Gets all shipped Wist dialect presets.
    /// </summary>
    public static IReadOnlyList<WistShippedDialectPreset> All { get; } =
    [
        FullDefault,
        FullDefaultNative,
        MinimalArithmetic,
        MinimalArithmeticNative,
        PricingRestricted,
        Ssa,
        CompositionRestricted
    ];

    /// <summary>
    ///     Attempts to get a shipped Wist dialect preset by identifier.
    /// </summary>
    public static bool TryGet(string presetId, [NotNullWhen(true)] out WistShippedDialectPreset? preset)
    {
        if (string.IsNullOrWhiteSpace(presetId))
        {
            preset = null;
            return false;
        }

        preset = All.FirstOrDefault(x => string.Equals(x.Id, presetId, StringComparison.OrdinalIgnoreCase));
        return preset != null;
    }

    /// <summary>
    ///     Gets a shipped Wist dialect preset by identifier or throws when it is not known.
    /// </summary>
    public static WistShippedDialectPreset GetRequired(string presetId)
    {
        if (TryGet(presetId, out var preset))
            return preset;

        return Thrower.ArgumentOutOfRange<WistShippedDialectPreset>(
            nameof(presetId),
            $"Unknown shipped Wist dialect preset '{presetId}'.");
    }

    private static WistShippedDialectPreset Create(string id, string displayName, string description)
        => new(
            id,
            Path.Combine("Dialects", "examples", "wist", id, "dialect.wistdialect"),
            displayName,
            description);
}