using System.Diagnostics.CodeAnalysis;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Wist.LanguagePack;

public static class WistLanguageDefinitions
{
    public const string FullDefaultId = "full-default";
    public const string FullDefaultNativeId = "full-default-native";
    public const string FunctionCallsSafeMathId = "function-calls-safe-math";
    public const string MinimalArithmeticId = "minimal-arithmetic";
    public const string MinimalArithmeticGroupedId = "minimal-arithmetic-grouped";
    public const string MinimalArithmeticNativeId = "minimal-arithmetic-native";
    public const string PricingRestrictedId = "pricing-restricted";
    public const string SsaId = "ssa";
    public const string CompositionRestrictedId = "composition-restricted";

    private static readonly IReadOnlyDictionary<string, PresetDefinition> Presets =
        new Dictionary<string, PresetDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [FullDefaultId] = new(
                [WistFeatureIds.Arithmetic, WistFeatureIds.BooleanLogic, WistFeatureIds.Comments,
                 WistFeatureIds.Comparisons, WistFeatureIds.ConditionalControlFlow, WistFeatureIds.CSharpInterop,
                 WistFeatureIds.Equality, WistFeatureIds.Identifiers, WistFeatureIds.Labels, WistFeatureIds.Loops,
                 WistFeatureIds.Numbers, WistFeatureIds.Scopes, WistFeatureIds.SemicolonAsNewLine,
                 WistFeatureIds.Variables, WistFeatureIds.Whitespaces,
                 WistFeatureIds.BooleanOptimization, WistFeatureIds.ComparisonIntrinsicOptimization],
                ["cil", "interpreter"], WistInternalFeatureIds.TrustedSecurity, AllowHostInterop: true),
            [FullDefaultNativeId] = new(
                [WistFeatureIds.BooleanLogic, WistFeatureIds.Comments, WistFeatureIds.Comparisons,
                 WistFeatureIds.ConditionalControlFlow, WistFeatureIds.CSharpInterop, WistFeatureIds.Equality,
                 WistFeatureIds.Identifiers, WistFeatureIds.Labels, WistFeatureIds.Loops, WistFeatureIds.NativeTypes,
                 WistFeatureIds.Scopes, WistFeatureIds.SemicolonAsNewLine, WistFeatureIds.Variables,
                 WistFeatureIds.Whitespaces, WistFeatureIds.ArithmeticOptimization, WistFeatureIds.BooleanOptimization,
                 WistFeatureIds.ComparisonIntrinsicOptimization, WistFeatureIds.EGraphOptimization,
                 WistFeatureIds.NativeCilOptimization, WistFeatureIds.NativeTypesOptimization],
                ["cil", "interpreter"], WistInternalFeatureIds.TrustedSecurity, AllowHostInterop: true),
            [FunctionCallsSafeMathId] = new(
                [WistFeatureIds.Arithmetic, WistFeatureIds.BooleanLogic, WistFeatureIds.Comments,
                 WistFeatureIds.Comparisons, WistFeatureIds.ConditionalControlFlow, WistFeatureIds.Equality,
                 WistFeatureIds.FunctionCalls, WistFeatureIds.Identifiers, WistFeatureIds.Numbers,
                 WistFeatureIds.SafeMathFunctions, WistFeatureIds.Scopes, WistFeatureIds.SemicolonAsNewLine,
                 WistFeatureIds.Variables, WistFeatureIds.Whitespaces, WistFeatureIds.BooleanOptimization,
                 WistFeatureIds.ComparisonIntrinsicOptimization],
                ["cil", "interpreter"], WistInternalFeatureIds.RestrictedSecurity, AllowHostInterop: false),
            [MinimalArithmeticId] = new(
                [WistFeatureIds.Arithmetic, WistFeatureIds.Numbers, WistFeatureIds.Scopes, WistFeatureIds.Whitespaces],
                ["interpreter"], WistInternalFeatureIds.RestrictedSecurity, AllowHostInterop: false),
            [MinimalArithmeticGroupedId] = new(
                [WistFeatureIds.Arithmetic, WistFeatureIds.Numbers, WistFeatureIds.Scopes, WistFeatureIds.Whitespaces],
                ["interpreter"], WistInternalFeatureIds.RestrictedSecurity, AllowHostInterop: false),
            [MinimalArithmeticNativeId] = new(
                [WistFeatureIds.NativeTypes, WistFeatureIds.Numbers, WistFeatureIds.Scopes, WistFeatureIds.Whitespaces,
                 WistFeatureIds.ArithmeticOptimization, WistFeatureIds.EGraphOptimization,
                 WistFeatureIds.NativeCilOptimization, WistFeatureIds.NativeTypesOptimization],
                ["cil"], WistInternalFeatureIds.RestrictedSecurity, AllowHostInterop: false),
            [PricingRestrictedId] = new(
                [WistFeatureIds.Identifiers, WistFeatureIds.NativeTypes, WistFeatureIds.Scopes,
                 WistFeatureIds.Variables, WistFeatureIds.Whitespaces, WistFeatureIds.ArithmeticOptimization,
                 WistFeatureIds.EGraphOptimization, WistFeatureIds.NativeCilOptimization,
                 WistFeatureIds.NativeTypesOptimization],
                ["cil", "interpreter"], WistInternalFeatureIds.RestrictedSecurity, AllowHostInterop: false),
            [SsaId] = new(
                [WistFeatureIds.Identifiers, WistFeatureIds.NativeTypes, WistFeatureIds.Scopes,
                 WistFeatureIds.Variables, WistFeatureIds.Whitespaces, WistFeatureIds.ArithmeticOptimization,
                 WistFeatureIds.EGraphOptimization, WistFeatureIds.NativeCilOptimization,
                 WistFeatureIds.NativeTypesOptimization, WistFeatureIds.SsaOptimization],
                ["cil", "interpreter"], WistInternalFeatureIds.RestrictedSecurity, AllowHostInterop: false),
            [CompositionRestrictedId] = new(
                [WistFeatureIds.Arithmetic, WistFeatureIds.BooleanLogic, WistFeatureIds.Comments,
                 WistFeatureIds.Comparisons, WistFeatureIds.ConditionalControlFlow, WistFeatureIds.Equality,
                 WistFeatureIds.Numbers, WistFeatureIds.Scopes, WistFeatureIds.Whitespaces],
                ["interpreter"], WistInternalFeatureIds.RestrictedSecurity, AllowHostInterop: false,
                CompositionRestricted: true)
        };

    public static IReadOnlyCollection<string> PresetIds => Presets.Keys.OrderBy(static x => x, StringComparer.Ordinal).ToArray();

    public static LanguageDefinition Create(string presetId)
    {
        if (!TryCreate(presetId, out var definition))
            throw new ArgumentOutOfRangeException(nameof(presetId), presetId, "Unknown shipped Wist preset.");
        return definition;
    }

    public static bool TryCreate(string presetId, [NotNullWhen(true)] out LanguageDefinition? definition)
    {
        if (string.IsNullOrWhiteSpace(presetId))
        {
            definition = null;
            return false;
        }

        var canonicalPresetId = Presets.Keys.FirstOrDefault(
            candidate => string.Equals(candidate, presetId, StringComparison.OrdinalIgnoreCase));
        if (canonicalPresetId == null)
        {
            definition = null;
            return false;
        }
        var preset = Presets[canonicalPresetId];

        var builder = LanguageDefinitionBuilder
            .Create($"wist.{canonicalPresetId}", WistLanguageFeaturePackage.PackageVersion.Value)
            .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
            .WithRuntimePolicy(new LanguageRuntimePolicy(AllowHostInterop: preset.AllowHostInterop))
            .WithMetadata("wist.preset", canonicalPresetId)
            .UseFeature(preset.SecurityFeature)
            .UseFeature(canonicalPresetId == SsaId
                ? WistSsaPolicyFeatureIds.Require
                : WistSsaPolicyFeatureIds.Disabled);

        if (preset.CompositionRestricted)
            builder.UseFeature(WistInternalFeatureIds.CompositionRestricted);
        foreach (var feature in preset.Features)
            builder.UseFeature(feature);
        foreach (var backend in preset.Backends)
            builder.EnableBackend(backend);

        definition = builder.Build();
        return true;
    }

    private sealed record PresetDefinition(
        IReadOnlyList<LanguageFeatureId> Features,
        IReadOnlyList<string> Backends,
        LanguageFeatureId SecurityFeature,
        bool AllowHostInterop,
        bool CompositionRestricted = false);
}
