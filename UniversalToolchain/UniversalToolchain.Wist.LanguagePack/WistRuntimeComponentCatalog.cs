using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.Wist.LanguagePack;

internal enum WistRuntimeComponentKind
{
    Module,
    Optimizer
}

internal sealed record WistRuntimeComponentDescriptor(
    LanguageContributionId ContributionId,
    string Alias,
    int Order,
    WistRuntimeComponentKind Kind);

internal static class WistInternalFeatureIds
{
    public static LanguageFeatureId TrustedSecurity { get; } = new("wist.policy.security.trusted");
    public static LanguageFeatureId RestrictedSecurity { get; } = new("wist.policy.security.restricted");
    public static LanguageFeatureId CompositionRestricted { get; } = new("wist.policy.composition-restricted");
}

internal static class WistRuntimeComponentCatalog
{
    public static IReadOnlyList<WistRuntimeComponentDescriptor> Modules { get; } =
    [
        Module(WistContributionIds.ArithmeticModule, "Arithmetic", 10),
        Module(WistContributionIds.BooleanLogicModule, "BooleanConditions", 20),
        Module(WistContributionIds.CSharpInteropModule, "CSharpInterop", 30),
        Module(WistContributionIds.CommentsModule, "Comments", 40),
        Module(WistContributionIds.ComparisonsModule, "ComparisonConditions", 50),
        Module(WistContributionIds.ConditionalControlFlowModule, "Conditions", 60),
        Module(WistContributionIds.EqualityModule, "Equality", 70),
        Module(WistContributionIds.FunctionCallsModule, "FunctionCalls", 80),
        Module(WistContributionIds.IdentifiersModule, "Identifier", 90),
        Module(WistContributionIds.InternalPreprocessorLexemesModule, "InternalPreprocessorLexemes", 100),
        Module(WistContributionIds.LabelsModule, "Labels", 110),
        Module(WistContributionIds.LoopsModule, "Loops", 120),
        Module(WistContributionIds.NativeTypesModule, "NativeTypes", 130),
        Module(WistContributionIds.NumbersModule, "Numbers", 140),
        Module(WistContributionIds.ParametersSetterModule, "ParametersSetter", 150),
        Module(WistContributionIds.SafeMathFunctionsModule, "SafeMathFunctions", 160),
        Module(WistContributionIds.ScopesModule, "Scopes", 170),
        Module(WistContributionIds.SemicolonAsNewLineModule, "SemicolonAsNewLine", 180),
        Module(WistContributionIds.TextualAdditionModule, "TextualAddition", 190),
        Module(WistContributionIds.VariablesModule, "Variables", 200),
        Module(WistContributionIds.WhitespacesModule, "Whitespaces", 210)
    ];

    public static IReadOnlyList<WistRuntimeComponentDescriptor> Optimizers { get; } =
    [
        Optimizer(WistContributionIds.ArithmeticOptimizer, "ArithmeticOptimization", 10),
        Optimizer(WistContributionIds.BooleanOptimizer, "BooleanOptimization", 20),
        Optimizer(WistContributionIds.ComparisonIntrinsicOptimizer, "ComparisonIntrinsicOptimization", 30),
        Optimizer(WistContributionIds.EGraphOptimizer, "EGraphOptimization", 40),
        Optimizer(WistContributionIds.NativeCilOptimizer, "NativeCilOptimization", 50),
        Optimizer(WistContributionIds.NativeTypesOptimizer, "NativeTypesOptimization", 60),
        Optimizer(WistContributionIds.SsaOptimizer, "Ssa", 70)
    ];

    private static readonly IReadOnlyDictionary<LanguageContributionId, WistRuntimeComponentDescriptor> ByContributionId =
        Modules.Concat(Optimizers).ToDictionary(static component => component.ContributionId);

    public static WistRuntimeComponentDescriptor GetRequired(
        LanguageContributionId contributionId,
        WistRuntimeComponentKind expectedKind)
    {
        if (!ByContributionId.TryGetValue(contributionId, out var component) || component.Kind != expectedKind)
        {
            throw new InvalidOperationException(
                $"Wist runtime contribution '{contributionId.Value}' is not a canonical {expectedKind.ToString().ToLowerInvariant()} component.");
        }
        return component;
    }

    private static WistRuntimeComponentDescriptor Module(
        LanguageContributionId contributionId,
        string alias,
        int order) => new(contributionId, alias, order, WistRuntimeComponentKind.Module);

    private static WistRuntimeComponentDescriptor Optimizer(
        LanguageContributionId contributionId,
        string alias,
        int order) => new(contributionId, alias, order, WistRuntimeComponentKind.Optimizer);
}
