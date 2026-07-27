using System.Diagnostics.CodeAnalysis;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist.Presets;

/// <summary>Provides the exact catalog of executable Wist dialect profiles shipped with the runtime.</summary>
public static class WistShippedDialectPresets
{
    public static WistShippedDialectPreset Default => FullDefault;

    public static WistShippedDialectPreset FullDefault { get; } = Create(
        "full-default", "Full default",
        "General-purpose Wist dialect with CIL and interpreter backends.",
        "cil", "cil", "interpreter");

    public static WistShippedDialectPreset FullDefaultNative { get; } = Create(
        "full-default-native", "Full default native",
        "General-purpose Wist dialect with native runtime features.",
        "cil", "cil", "interpreter");

    public static WistShippedDialectPreset FunctionCallsSafeMath { get; } = Create(
        "function-calls-safe-math", "Function calls safe math",
        "Restricted function-call profile exposing the approved safe-math catalog.",
        "cil", "cil", "interpreter");

    public static WistShippedDialectPreset MinimalArithmetic { get; } = Create(
        "minimal-arithmetic", "Minimal arithmetic",
        "Small interpreter-only arithmetic dialect.",
        "interpreter", "interpreter");

    public static WistShippedDialectPreset MinimalArithmeticGrouped { get; } = Create(
        "minimal-arithmetic-grouped", "Minimal arithmetic grouped",
        "Small interpreter-only arithmetic dialect assembled through the ArithmeticCore group.",
        "interpreter", "interpreter");

    public static WistShippedDialectPreset MinimalArithmeticNative { get; } = Create(
        "minimal-arithmetic-native", "Minimal arithmetic native",
        "Small CIL-only arithmetic dialect with native runtime features.",
        "cil", "cil");

    public static WistShippedDialectPreset PricingRestricted { get; } = Create(
        "pricing-restricted", "Pricing restricted",
        "Restricted Wist dialect for pricing formulas.",
        "cil", "cil", "interpreter");

    public static WistShippedDialectPreset Ssa { get; } = Create(
        "ssa", "SSA",
        "Restricted arithmetic Wist dialect with verifier-gated SSA optimization enabled.",
        "cil", "cil", "interpreter");

    public static WistShippedDialectPreset CompositionRestricted { get; } = Create(
        "composition-restricted", "Composition restricted",
        "Composition-constrained interpreter-only Wist dialect; not a process-isolation boundary.",
        "interpreter", "interpreter");

    public static IReadOnlyList<WistShippedDialectPreset> All { get; } =
    [
        FullDefault,
        FullDefaultNative,
        FunctionCallsSafeMath,
        MinimalArithmetic,
        MinimalArithmeticGrouped,
        MinimalArithmeticNative,
        PricingRestricted,
        Ssa,
        CompositionRestricted
    ];

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

    public static WistShippedDialectPreset GetRequired(string presetId)
    {
        if (TryGet(presetId, out var preset))
            return preset;

        return Thrower.ArgumentOutOfRange<WistShippedDialectPreset>(
            nameof(presetId),
            $"Unknown shipped Wist dialect preset '{presetId}'.");
    }

    private static WistShippedDialectPreset Create(
        string id,
        string displayName,
        string description,
        string defaultBackend,
        params string[] supportedBackends)
        => new(
            id,
            Path.Combine("Dialects", "examples", "wist", id, "dialect.wistdialect"),
            displayName,
            description,
            defaultBackend,
            supportedBackends);
}
