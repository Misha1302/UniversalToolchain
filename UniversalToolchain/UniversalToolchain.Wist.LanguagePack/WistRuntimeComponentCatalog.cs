using ArithmeticModule.Module;
using CommentsModule;
using ConditionsModule.Enums;
using ConditionsModule.Module;
using CSharpInteropModule.Module;
using EqualityModule;
using FunctionCallsModule;
using IdentifierModule;
using InternalPreprocessorLexemesModule;
using LabelsModule.Module;
using LoopsModule.Module;
using Microsoft.Extensions.DependencyInjection;
using NativeMathModule;
using NumbersModule.Module;
using ParametersSetterModule;
using SafeMathFunctionsModule;
using ScopesModule.Module;
using SemicolonAsNewLineModule;
using UniversalToolchain.Language.Abstractions;
using VariablesModule;
using WhitespacesModule;

namespace UniversalToolchain.Wist.LanguagePack;

internal enum WistRuntimeComponentKind
{
    Module,
    Optimizer
}

internal sealed record WistRuntimeComponentDescriptor(
    LanguageContributionId ContributionId,
    LanguageFeatureId FeatureId,
    string Alias,
    int Order,
    WistRuntimeComponentKind Kind,
    Func<IServiceProvider, object>? ModuleFactory = null);

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
        Module<ArithmeticModuleImpl>(WistContributionIds.ArithmeticModule, WistFeatureIds.Arithmetic, "Arithmetic", 10),
        Module<BooleanOperations>(WistContributionIds.BooleanLogicModule, WistFeatureIds.BooleanLogic, "BooleanConditions", 20),
        Module<CSharpInteropModuleImpl>(WistContributionIds.CSharpInteropModule, WistFeatureIds.CSharpInterop, "CSharpInterop", 30),
        Module<CommentsModuleImpl>(WistContributionIds.CommentsModule, WistFeatureIds.Comments, "Comments", 40),
        Module<ComparisonOperations>(WistContributionIds.ComparisonsModule, WistFeatureIds.Comparisons, "ComparisonConditions", 50),
        Module<ConditionsModuleImpl>(WistContributionIds.ConditionalControlFlowModule, WistFeatureIds.ConditionalControlFlow, "Conditions", 60),
        Module<EqualityModuleImpl>(WistContributionIds.EqualityModule, WistFeatureIds.Equality, "Equality", 70),
        Module<FunctionCallsModuleImpl>(WistContributionIds.FunctionCallsModule, WistFeatureIds.FunctionCalls, "FunctionCalls", 80),
        Module<IdentifierModuleImpl>(WistContributionIds.IdentifiersModule, WistFeatureIds.Identifiers, "Identifier", 90),
        Module<InternalPreprocessorLexemesModuleImpl>(WistContributionIds.InternalPreprocessorLexemesModule, WistFeatureIds.InternalPreprocessorLexemes, "InternalPreprocessorLexemes", 100),
        Module<LabelsModuleImpl>(WistContributionIds.LabelsModule, WistFeatureIds.Labels, "Labels", 110),
        Module<LoopsModuleImpl>(WistContributionIds.LoopsModule, WistFeatureIds.Loops, "Loops", 120),
        Module<NativeTypesModuleImpl>(WistContributionIds.NativeTypesModule, WistFeatureIds.NativeTypes, "NativeTypes", 130),
        Module<NumbersModuleImpl>(WistContributionIds.NumbersModule, WistFeatureIds.Numbers, "Numbers", 140),
        Module<ParametersSetterModuleImpl>(WistContributionIds.ParametersSetterModule, WistFeatureIds.ParametersSetter, "ParametersSetter", 150),
        Module<SafeMathFunctionsModuleImpl>(WistContributionIds.SafeMathFunctionsModule, WistFeatureIds.SafeMathFunctions, "SafeMathFunctions", 160),
        Module<ScopesModuleImpl>(WistContributionIds.ScopesModule, WistFeatureIds.Scopes, "Scopes", 170),
        Module<SemicolonAsNewLineModuleImpl>(WistContributionIds.SemicolonAsNewLineModule, WistFeatureIds.SemicolonAsNewLine, "SemicolonAsNewLine", 180),
        Module<TextualAdditionModuleImpl>(WistContributionIds.TextualAdditionModule, WistFeatureIds.TextualAddition, "TextualAddition", 190),
        Module<VariablesModuleImpl>(WistContributionIds.VariablesModule, WistFeatureIds.Variables, "Variables", 200),
        Module<WhitespaceModuleImpl>(WistContributionIds.WhitespacesModule, WistFeatureIds.Whitespaces, "Whitespaces", 210)
    ];

    public static IReadOnlyList<WistRuntimeComponentDescriptor> Optimizers { get; } =
    [
        Optimizer(WistContributionIds.ArithmeticOptimizer, WistFeatureIds.ArithmeticOptimization, "ArithmeticOptimization", 10),
        Optimizer(WistContributionIds.BooleanOptimizer, WistFeatureIds.BooleanOptimization, "BooleanOptimization", 20),
        Optimizer(WistContributionIds.ComparisonIntrinsicOptimizer, WistFeatureIds.ComparisonIntrinsicOptimization, "ComparisonIntrinsicOptimization", 30),
        Optimizer(WistContributionIds.EGraphOptimizer, WistFeatureIds.EGraphOptimization, "EGraphOptimization", 40),
        Optimizer(WistContributionIds.NativeCilOptimizer, WistFeatureIds.NativeCilOptimization, "NativeCilOptimization", 50),
        Optimizer(WistContributionIds.NativeTypesOptimizer, WistFeatureIds.NativeTypesOptimization, "NativeTypesOptimization", 60),
        Optimizer(WistContributionIds.SsaOptimizer, WistFeatureIds.SsaOptimization, "Ssa", 70)
    ];

    private static readonly IReadOnlyDictionary<LanguageContributionId, WistRuntimeComponentDescriptor> ByContributionId =
        Modules.Concat(Optimizers).ToDictionary(static component => component.ContributionId);
    private static readonly IReadOnlyDictionary<string, WistRuntimeComponentDescriptor> ByAlias =
        Modules.Concat(Optimizers).ToDictionary(static component => component.Alias, StringComparer.Ordinal);

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

    public static WistRuntimeComponentDescriptor GetRequiredAlias(
        string alias,
        WistRuntimeComponentKind expectedKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        if (!ByAlias.TryGetValue(alias, out var component) || component.Kind != expectedKind)
        {
            throw new InvalidOperationException(
                $"Wist alias '{alias}' is not a canonical {expectedKind.ToString().ToLowerInvariant()} component.");
        }
        return component;
    }

    public static bool TryGetAlias(
        string alias,
        WistRuntimeComponentKind expectedKind,
        out WistRuntimeComponentDescriptor? component)
    {
        component = null;
        if (string.IsNullOrWhiteSpace(alias))
            return false;
        if (!ByAlias.TryGetValue(alias, out var candidate) || candidate.Kind != expectedKind)
            return false;
        component = candidate;
        return true;
    }

    public static bool IsCanonicalModule(LanguageContributionId contributionId) =>
        ByContributionId.TryGetValue(contributionId, out var component) && component.Kind == WistRuntimeComponentKind.Module;

    private static WistRuntimeComponentDescriptor Module<TModule>(
        LanguageContributionId contributionId,
        LanguageFeatureId featureId,
        string alias,
        int order) where TModule : class =>
        new(
            contributionId,
            featureId,
            alias,
            order,
            WistRuntimeComponentKind.Module,
            static services => ActivatorUtilities.CreateInstance<TModule>(services));

    private static WistRuntimeComponentDescriptor Optimizer(
        LanguageContributionId contributionId,
        LanguageFeatureId featureId,
        string alias,
        int order) => new(contributionId, featureId, alias, order, WistRuntimeComponentKind.Optimizer);
}
