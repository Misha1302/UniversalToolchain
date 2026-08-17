using ArithmeticModule.Module;
using BasicCore.Builtins;
using BasicCore.Contracts;
using CommentsModule;
using ConditionsModule.Enums;
using ConditionsModule.Module;
using ConditionsModule.Optimizers;
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
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Ssa.Optimization;
using VariablesModule;
using WhitespacesModule;

namespace UniversalToolchain.Wist.LanguagePack;

internal enum WistRuntimeComponentKind
{
    Module,
    Optimizer
}

[Flags]
internal enum WistFrontendPhaseRoles
{
    None = 0,
    Syntax = 1 << 0,
    Semantics = 1 << 1,
    Lowering = 1 << 2
}

internal sealed record WistRuntimeComponentDescriptor(
    LanguageContributionId ContributionId,
    LanguageFeatureId FeatureId,
    string Alias,
    int Order,
    WistRuntimeComponentKind Kind,
    WistFrontendPhaseRoles FrontendPhaseRoles,
    Func<Type> ImplementationTypeFactory,
    Func<IServiceProvider, object>? ModuleFactory = null)
{
    public Type ImplementationType => ImplementationTypeFactory();
}

internal static class WistInternalFeatureIds
{
    public static LanguageFeatureId TrustedSecurity { get; } = new("wist.policy.security.trusted");
    public static LanguageFeatureId RestrictedSecurity { get; } = new("wist.policy.security.restricted");
    public static LanguageFeatureId CompositionRestricted { get; } = new("wist.policy.composition-restricted");
}

internal static class WistRuntimeComponentCatalog
{
    private const WistFrontendPhaseRoles SyntaxOnly = WistFrontendPhaseRoles.Syntax;
    private const WistFrontendPhaseRoles SyntaxAndLowering =
        WistFrontendPhaseRoles.Syntax | WistFrontendPhaseRoles.Lowering;
    private const WistFrontendPhaseRoles SyntaxSemanticsAndLowering =
        WistFrontendPhaseRoles.Syntax | WistFrontendPhaseRoles.Semantics | WistFrontendPhaseRoles.Lowering;

    public static IReadOnlyList<WistRuntimeComponentDescriptor> Modules { get; } =
    [
        Module(WistContributionIds.ArithmeticModule, WistFeatureIds.Arithmetic, "Arithmetic", 10, SyntaxAndLowering,
            static () => typeof(ArithmeticModuleImpl),
            static services => ActivatorUtilities.CreateInstance<ArithmeticModuleImpl>(services)),
        Module(WistContributionIds.BooleanLogicModule, WistFeatureIds.BooleanLogic, "BooleanConditions", 20, SyntaxAndLowering,
            static () => typeof(BooleanOperations),
            static services => ActivatorUtilities.CreateInstance<BooleanOperations>(services)),
        Module(WistContributionIds.CSharpInteropModule, WistFeatureIds.CSharpInterop, "CSharpInterop", 30, SyntaxAndLowering,
            static () => typeof(CSharpInteropModuleImpl),
            static services => ActivatorUtilities.CreateInstance<CSharpInteropModuleImpl>(services)),
        Module(WistContributionIds.CommentsModule, WistFeatureIds.Comments, "Comments", 40, SyntaxOnly,
            static () => typeof(CommentsModuleImpl),
            static services => ActivatorUtilities.CreateInstance<CommentsModuleImpl>(services)),
        Module(WistContributionIds.ComparisonsModule, WistFeatureIds.Comparisons, "ComparisonConditions", 50, SyntaxAndLowering,
            static () => typeof(ComparisonOperations),
            static services => ActivatorUtilities.CreateInstance<ComparisonOperations>(services)),
        Module(WistContributionIds.ConditionalControlFlowModule, WistFeatureIds.ConditionalControlFlow, "Conditions", 60, SyntaxAndLowering,
            static () => typeof(ConditionsModuleImpl),
            static services => ActivatorUtilities.CreateInstance<ConditionsModuleImpl>(services)),
        Module(WistContributionIds.EqualityModule, WistFeatureIds.Equality, "Equality", 70, SyntaxAndLowering,
            static () => typeof(EqualityModuleImpl),
            static services => ActivatorUtilities.CreateInstance<EqualityModuleImpl>(services)),
        Module(WistContributionIds.FunctionCallsModule, WistFeatureIds.FunctionCalls, "FunctionCalls", 80, SyntaxAndLowering,
            static () => typeof(FunctionCallsModuleImpl),
            static services => ActivatorUtilities.CreateInstance<FunctionCallsModuleImpl>(services)),
        Module(WistContributionIds.IdentifiersModule, WistFeatureIds.Identifiers, "Identifier", 90, SyntaxOnly,
            static () => typeof(IdentifierModuleImpl),
            static services => ActivatorUtilities.CreateInstance<IdentifierModuleImpl>(services)),
        Module(WistContributionIds.InternalPreprocessorLexemesModule, WistFeatureIds.InternalPreprocessorLexemes, "InternalPreprocessorLexemes", 100, SyntaxOnly,
            static () => typeof(InternalPreprocessorLexemesModuleImpl),
            static services => ActivatorUtilities.CreateInstance<InternalPreprocessorLexemesModuleImpl>(services)),
        Module(WistContributionIds.LabelsModule, WistFeatureIds.Labels, "Labels", 110, SyntaxAndLowering,
            static () => typeof(LabelsModuleImpl),
            static services => ActivatorUtilities.CreateInstance<LabelsModuleImpl>(services)),
        Module(WistContributionIds.LoopsModule, WistFeatureIds.Loops, "Loops", 120, SyntaxAndLowering,
            static () => typeof(LoopsModuleImpl),
            static services => ActivatorUtilities.CreateInstance<LoopsModuleImpl>(services)),
        Module(WistContributionIds.NativeTypesModule, WistFeatureIds.NativeTypes, "NativeTypes", 130, SyntaxAndLowering,
            static () => typeof(NativeTypesModuleImpl),
            static services => ActivatorUtilities.CreateInstance<NativeTypesModuleImpl>(services)),
        Module(WistContributionIds.NumbersModule, WistFeatureIds.Numbers, "Numbers", 140, SyntaxAndLowering,
            static () => typeof(NumbersModuleImpl),
            static services => ActivatorUtilities.CreateInstance<NumbersModuleImpl>(services)),
        Module(WistContributionIds.ParametersSetterModule, WistFeatureIds.ParametersSetter, "ParametersSetter", 150, SyntaxOnly,
            static () => typeof(ParametersSetterModuleImpl),
            static services => ActivatorUtilities.CreateInstance<ParametersSetterModuleImpl>(services)),
        Module(WistContributionIds.SafeMathFunctionsModule, WistFeatureIds.SafeMathFunctions, "SafeMathFunctions", 160, SyntaxOnly,
            static () => typeof(SafeMathFunctionsModuleImpl),
            static services => ActivatorUtilities.CreateInstance<SafeMathFunctionsModuleImpl>(services)),
        Module(WistContributionIds.ScopesModule, WistFeatureIds.Scopes, "Scopes", 170, SyntaxAndLowering,
            static () => typeof(ScopesModuleImpl),
            static services => ActivatorUtilities.CreateInstance<ScopesModuleImpl>(services)),
        Module(WistContributionIds.SemicolonAsNewLineModule, WistFeatureIds.SemicolonAsNewLine, "SemicolonAsNewLine", 180, SyntaxOnly,
            static () => typeof(SemicolonAsNewLineModuleImpl),
            static services => ActivatorUtilities.CreateInstance<SemicolonAsNewLineModuleImpl>(services)),
        Module(WistContributionIds.TextualAdditionModule, WistFeatureIds.TextualAddition, "TextualAddition", 190, SyntaxOnly,
            static () => typeof(TextualAdditionModuleImpl),
            static services => ActivatorUtilities.CreateInstance<TextualAdditionModuleImpl>(services)),
        Module(WistContributionIds.VariablesModule, WistFeatureIds.Variables, "Variables", 200, SyntaxSemanticsAndLowering,
            static () => typeof(VariablesModuleImpl),
            static services => ActivatorUtilities.CreateInstance<VariablesModuleImpl>(services)),
        Module(WistContributionIds.WhitespacesModule, WistFeatureIds.Whitespaces, "Whitespaces", 210, SyntaxOnly,
            static () => typeof(WhitespaceModuleImpl),
            static services => ActivatorUtilities.CreateInstance<WhitespaceModuleImpl>(services))
    ];

    public static IReadOnlyList<WistRuntimeComponentDescriptor> Optimizers { get; } =
    [
        Optimizer(WistContributionIds.ArithmeticOptimizer, WistFeatureIds.ArithmeticOptimization, "ArithmeticOptimization", 10,
            static () => typeof(ArithmeticOptimizerModule)),
        Optimizer(WistContributionIds.BooleanOptimizer, WistFeatureIds.BooleanOptimization, "BooleanOptimization", 20,
            static () => typeof(BooleanOptimizerModule)),
        Optimizer(WistContributionIds.ComparisonIntrinsicOptimizer, WistFeatureIds.ComparisonIntrinsicOptimization, "ComparisonIntrinsicOptimization", 30,
            static () => typeof(ComparisonIntrinsicOptimizerModule)),
        Optimizer(WistContributionIds.EGraphOptimizer, WistFeatureIds.EGraphOptimization, "EGraphOptimization", 40,
            static () => typeof(EGraphOptimizerModule)),
        Optimizer(WistContributionIds.NativeCilOptimizer, WistFeatureIds.NativeCilOptimization, "NativeCilOptimization", 50,
            static () => typeof(NativeCilOptimizerModule)),
        Optimizer(WistContributionIds.NativeTypesOptimizer, WistFeatureIds.NativeTypesOptimization, "NativeTypesOptimization", 60,
            static () => typeof(NativeTypesOptimizerModule)),
        Optimizer(WistContributionIds.SsaOptimizer, WistFeatureIds.SsaOptimization, "Ssa", 70,
            static () => typeof(SsaOptimizerModule))
    ];

    private static readonly IReadOnlyDictionary<LanguageContributionId, Func<IIntrinsicDescriptorProvider>>
        IntrinsicDescriptorProvidersByContribution = new Dictionary<LanguageContributionId, Func<IIntrinsicDescriptorProvider>>
        {
            [WistContributionIds.ArithmeticOptimizer] = static () => new ArithmeticIntrinsicDescriptorProvider(),
            [WistContributionIds.BooleanOptimizer] = static () => new BooleanIntrinsicDescriptorProvider(),
            [WistContributionIds.ComparisonIntrinsicOptimizer] = static () => new ComparisonIntrinsicDescriptorProvider(),
            [WistContributionIds.EGraphOptimizer] = static () => new ArithmeticIntrinsicDescriptorProvider()
        };

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

    public static IReadOnlyList<Type> GetSelectedImplementationTypes(LanguagePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return plan.Contributions
            .Select(static contribution => contribution.Contribution.Id)
            .Where(ByContributionId.ContainsKey)
            .Select(id => ByContributionId[id].ImplementationType)
            .Distinct()
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<IIntrinsicDescriptorProvider> CreateSelectedIntrinsicDescriptorProviders(LanguagePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var providers = new Dictionary<Type, IIntrinsicDescriptorProvider>
        {
            [typeof(CoreIntrinsicDescriptorProvider)] =
                new CoreIntrinsicDescriptorProvider(new MethodCallTypeSemanticsResolver())
        };

        foreach (var contributionId in plan.Contributions
                     .Select(static contribution => contribution.Contribution.Id)
                     .Distinct())
        {
            if (!IntrinsicDescriptorProvidersByContribution.TryGetValue(contributionId, out var factory))
                continue;

            var provider = factory();
            providers.TryAdd(provider.GetType(), provider);
        }

        return providers.Values
            .OrderBy(static provider => provider.GetType().FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static WistRuntimeComponentDescriptor Module(
        LanguageContributionId contributionId,
        LanguageFeatureId featureId,
        string alias,
        int order,
        WistFrontendPhaseRoles frontendPhaseRoles,
        Func<Type> implementationTypeFactory,
        Func<IServiceProvider, object> moduleFactory)
    {
        if ((frontendPhaseRoles & WistFrontendPhaseRoles.Syntax) == 0)
        {
            throw new InvalidOperationException(
                $"Canonical Wist module '{contributionId.Value}' must own the syntax phase represented by its primary contribution.");
        }

        return new WistRuntimeComponentDescriptor(
            contributionId,
            featureId,
            alias,
            order,
            WistRuntimeComponentKind.Module,
            frontendPhaseRoles,
            implementationTypeFactory,
            moduleFactory);
    }

    private static WistRuntimeComponentDescriptor Optimizer(
        LanguageContributionId contributionId,
        LanguageFeatureId featureId,
        string alias,
        int order,
        Func<Type> implementationTypeFactory) =>
        new(
            contributionId,
            featureId,
            alias,
            order,
            WistRuntimeComponentKind.Optimizer,
            WistFrontendPhaseRoles.None,
            implementationTypeFactory);
}
