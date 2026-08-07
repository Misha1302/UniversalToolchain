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
        Module<ArithmeticModuleImpl>(WistContributionIds.ArithmeticModule, "Arithmetic", 10),
        Module<BooleanOperations>(WistContributionIds.BooleanLogicModule, "BooleanConditions", 20),
        Module<CSharpInteropModuleImpl>(WistContributionIds.CSharpInteropModule, "CSharpInterop", 30),
        Module<CommentsModuleImpl>(WistContributionIds.CommentsModule, "Comments", 40),
        Module<ComparisonOperations>(WistContributionIds.ComparisonsModule, "ComparisonConditions", 50),
        Module<ConditionsModuleImpl>(WistContributionIds.ConditionalControlFlowModule, "Conditions", 60),
        Module<EqualityModuleImpl>(WistContributionIds.EqualityModule, "Equality", 70),
        Module<FunctionCallsModuleImpl>(WistContributionIds.FunctionCallsModule, "FunctionCalls", 80),
        Module<IdentifierModuleImpl>(WistContributionIds.IdentifiersModule, "Identifier", 90),
        Module<InternalPreprocessorLexemesModuleImpl>(WistContributionIds.InternalPreprocessorLexemesModule, "InternalPreprocessorLexemes", 100),
        Module<LabelsModuleImpl>(WistContributionIds.LabelsModule, "Labels", 110),
        Module<LoopsModuleImpl>(WistContributionIds.LoopsModule, "Loops", 120),
        Module<NativeTypesModuleImpl>(WistContributionIds.NativeTypesModule, "NativeTypes", 130),
        Module<NumbersModuleImpl>(WistContributionIds.NumbersModule, "Numbers", 140),
        Module<ParametersSetterModuleImpl>(WistContributionIds.ParametersSetterModule, "ParametersSetter", 150),
        Module<SafeMathFunctionsModuleImpl>(WistContributionIds.SafeMathFunctionsModule, "SafeMathFunctions", 160),
        Module<ScopesModuleImpl>(WistContributionIds.ScopesModule, "Scopes", 170),
        Module<SemicolonAsNewLineModuleImpl>(WistContributionIds.SemicolonAsNewLineModule, "SemicolonAsNewLine", 180),
        Module<TextualAdditionModuleImpl>(WistContributionIds.TextualAdditionModule, "TextualAddition", 190),
        Module<VariablesModuleImpl>(WistContributionIds.VariablesModule, "Variables", 200),
        Module<WhitespaceModuleImpl>(WistContributionIds.WhitespacesModule, "Whitespaces", 210)
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

    public static bool IsCanonicalModule(LanguageContributionId contributionId) =>
        ByContributionId.TryGetValue(contributionId, out var component) && component.Kind == WistRuntimeComponentKind.Module;

    private static WistRuntimeComponentDescriptor Module<TModule>(
        LanguageContributionId contributionId,
        string alias,
        int order) where TModule : class =>
        new(
            contributionId,
            alias,
            order,
            WistRuntimeComponentKind.Module,
            static services => ActivatorUtilities.CreateInstance<TModule>(services));

    private static WistRuntimeComponentDescriptor Optimizer(
        LanguageContributionId contributionId,
        string alias,
        int order) => new(contributionId, alias, order, WistRuntimeComponentKind.Optimizer);
}
