using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.Wist.LanguagePack;

public static class WistFeatureIds
{
    public static LanguageFeatureId Whitespaces { get; } = new("wist.whitespaces");
    public static LanguageFeatureId Scopes { get; } = new("wist.scopes");
    public static LanguageFeatureId Numbers { get; } = new("wist.numbers");
    public static LanguageFeatureId Arithmetic { get; } = new("wist.arithmetic");
    public static LanguageFeatureId Identifiers { get; } = new("wist.identifiers");
    public static LanguageFeatureId Variables { get; } = new("wist.variables");
    public static LanguageFeatureId Comparisons { get; } = new("wist.comparisons");
    public static LanguageFeatureId BooleanLogic { get; } = new("wist.boolean-logic");
    public static LanguageFeatureId ConditionalControlFlow { get; } = new("wist.conditional-control-flow");
    public static LanguageFeatureId Conditions { get; } = new("wist.conditions");
    public static LanguageFeatureId Comments { get; } = new("wist.comments");
    public static LanguageFeatureId CSharpInterop { get; } = new("wist.csharp-interop");
    public static LanguageFeatureId Equality { get; } = new("wist.equality");
    public static LanguageFeatureId FunctionCalls { get; } = new("wist.function-calls");
    public static LanguageFeatureId InternalPreprocessorLexemes { get; } = new("wist.internal-preprocessor-lexemes");
    public static LanguageFeatureId Labels { get; } = new("wist.labels");
    public static LanguageFeatureId Loops { get; } = new("wist.loops");
    public static LanguageFeatureId NativeTypes { get; } = new("wist.native-types");
    public static LanguageFeatureId ParametersSetter { get; } = new("wist.parameters-setter");
    public static LanguageFeatureId SafeMathFunctions { get; } = new("wist.safe-math-functions");
    public static LanguageFeatureId SemicolonAsNewLine { get; } = new("wist.semicolon-as-new-line");
    public static LanguageFeatureId TextualAddition { get; } = new("wist.textual-addition");
    public static LanguageFeatureId ArithmeticOptimization { get; } = new("wist.optimizer.arithmetic");
    public static LanguageFeatureId BooleanOptimization { get; } = new("wist.optimizer.boolean");
    public static LanguageFeatureId ComparisonIntrinsicOptimization { get; } = new("wist.optimizer.comparison-intrinsic");
    public static LanguageFeatureId EGraphOptimization { get; } = new("wist.optimizer.egraph");
    public static LanguageFeatureId NativeCilOptimization { get; } = new("wist.optimizer.native-cil");
    public static LanguageFeatureId NativeTypesOptimization { get; } = new("wist.optimizer.native-types");
    public static LanguageFeatureId SsaOptimization { get; } = new("wist.optimizer.ssa");
}

public static class WistContributionIds
{
    public static LanguageContributionId Frontend { get; } = new("wist.frontend.parser");
    public static LanguageContributionId LoweringToBytecode { get; } = new("wist.lowering.bytecode");
    public static LanguageContributionId LoweringToAir { get; } = new("wist.lowering.air");
    public static LanguageContributionId InterpreterBackend { get; } = new("wist.backend.interpreter");
    public static LanguageContributionId CilBackend { get; } = new("wist.backend.cil");
    public static LanguageContributionId RuntimeProvider { get; } = new("wist.runtime.provider");
    public static LanguageContributionId WhitespacesModule { get; } = new("wist.module.whitespaces");
    public static LanguageContributionId ScopesModule { get; } = new("wist.module.scopes");
    public static LanguageContributionId NumbersModule { get; } = new("wist.module.numbers");
    public static LanguageContributionId ArithmeticModule { get; } = new("wist.module.arithmetic");
    public static LanguageContributionId IdentifiersModule { get; } = new("wist.module.identifiers");
    public static LanguageContributionId VariablesModule { get; } = new("wist.module.variables");
    public static LanguageContributionId ComparisonsModule { get; } = new("wist.module.comparisons");
    public static LanguageContributionId BooleanLogicModule { get; } = new("wist.module.boolean-logic");
    public static LanguageContributionId ConditionalControlFlowModule { get; } = new("wist.module.conditional-control-flow");
    public static LanguageContributionId CommentsModule { get; } = new("wist.module.comments");
    public static LanguageContributionId CSharpInteropModule { get; } = new("wist.module.csharp-interop");
    public static LanguageContributionId EqualityModule { get; } = new("wist.module.equality");
    public static LanguageContributionId FunctionCallsModule { get; } = new("wist.module.function-calls");
    public static LanguageContributionId InternalPreprocessorLexemesModule { get; } = new("wist.module.internal-preprocessor-lexemes");
    public static LanguageContributionId LabelsModule { get; } = new("wist.module.labels");
    public static LanguageContributionId LoopsModule { get; } = new("wist.module.loops");
    public static LanguageContributionId NativeTypesModule { get; } = new("wist.module.native-types");
    public static LanguageContributionId ParametersSetterModule { get; } = new("wist.module.parameters-setter");
    public static LanguageContributionId SafeMathFunctionsModule { get; } = new("wist.module.safe-math-functions");
    public static LanguageContributionId SemicolonAsNewLineModule { get; } = new("wist.module.semicolon-as-new-line");
    public static LanguageContributionId TextualAdditionModule { get; } = new("wist.module.textual-addition");
    public static LanguageContributionId ArithmeticOptimizer { get; } = new("wist.optimizer.arithmetic");
    public static LanguageContributionId BooleanOptimizer { get; } = new("wist.optimizer.boolean");
    public static LanguageContributionId ComparisonIntrinsicOptimizer { get; } = new("wist.optimizer.comparison-intrinsic");
    public static LanguageContributionId EGraphOptimizer { get; } = new("wist.optimizer.egraph");
    public static LanguageContributionId NativeCilOptimizer { get; } = new("wist.optimizer.native-cil");
    public static LanguageContributionId NativeTypesOptimizer { get; } = new("wist.optimizer.native-types");
    public static LanguageContributionId SsaOptimizer { get; } = new("wist.optimizer.ssa");
}

public static class WistArtifactKinds
{
    public static LanguageArtifactKindId SyntaxTree { get; } = new("wist.syntax-tree");
    public static LanguageArtifactKindId Bytecode { get; } = new("wist.bytecode");
    public static LanguageArtifactKindId InterpreterArtifact { get; } = new("wist.interpreter.artifact");
    public static LanguageArtifactKindId CilArtifact { get; } = new("wist.cil-artifact");
    public static LanguageArtifactContract SyntaxTreeContract { get; } = new(SyntaxTree, "wist.syntax-tree/v1");
    public static LanguageArtifactContract BytecodeContract { get; } = new(Bytecode, "wist.bytecode/v1");
    public static LanguageArtifactContract AirContract { get; } = new(LanguageArtifacts.Air, "wist.air/v1");
    public static LanguageArtifactContract InterpreterArtifactContract { get; } = new(InterpreterArtifact, "wist.interpreter-artifact/v1");
    public static LanguageArtifactContract CilArtifactContract { get; } = new(CilArtifact, "wist.cil-artifact/v1");
}

public sealed class WistLanguageFeaturePackage : ILanguageExtensionPackage
{
    private static readonly BackendId Cil = new("cil");
    private static readonly BackendId Interpreter = new("interpreter");
    private static readonly BackendId[] BothBackends = [Cil, Interpreter];
    private static readonly LanguageSlotId SyntaxToBytecodeSlot = new("wist.lowering.syntax-to-bytecode");
    private static readonly LanguageSlotId BytecodeToAirSlot = new("wist.lowering.bytecode-to-air");

    public static LanguagePackageId PackageId { get; } = new("UniversalToolchain.Wist.LanguagePack");
    public static LanguageVersion PackageVersion { get; } = new(WistLanguagePackIdentity.Version);
    public static LanguageRuntimeProviderId RuntimeProviderId { get; } = new(PackageId.Value);

    public LanguagePackageDescriptor Descriptor { get; } = new(
        PackageId,
        PackageVersion,
        ToolchainApi.Current,
        CreateFeatures(),
        new Dictionary<string, string>
        {
            ["language"] = "Wist",
            ["status"] = "typed-authoring-runtime-provider",
            ["positioning"] = "Typed Wist authoring package over the canonical Wist dialect runtime with shipped-preset parity"
        },
        CreateContributions());

    private static IReadOnlyList<LanguageFeatureDescriptor> CreateFeatures() =>
    [
        Feature(WistFeatureIds.Whitespaces, WistContributionIds.WhitespacesModule),
        Feature(WistFeatureIds.Scopes, WistContributionIds.ScopesModule),
        Feature(WistFeatureIds.Numbers, WistContributionIds.NumbersModule),
        Feature(WistFeatureIds.Arithmetic, WistContributionIds.ArithmeticModule),
        Feature(WistFeatureIds.Identifiers, WistContributionIds.IdentifiersModule),
        Feature(WistFeatureIds.Variables, WistContributionIds.VariablesModule),
        Feature(WistFeatureIds.Comparisons, WistContributionIds.ComparisonsModule),
        Feature(WistFeatureIds.BooleanLogic, WistContributionIds.BooleanLogicModule),
        Feature(WistFeatureIds.ConditionalControlFlow, WistContributionIds.ConditionalControlFlowModule),
        Feature(WistFeatureIds.Conditions,
            [WistContributionIds.ComparisonsModule, WistContributionIds.BooleanLogicModule, WistContributionIds.ConditionalControlFlowModule]),
        Feature(WistFeatureIds.Comments, WistContributionIds.CommentsModule),
        Feature(WistFeatureIds.CSharpInterop, WistContributionIds.CSharpInteropModule),
        Feature(WistFeatureIds.Equality, WistContributionIds.EqualityModule),
        Feature(WistFeatureIds.FunctionCalls, WistContributionIds.FunctionCallsModule),
        Feature(WistFeatureIds.InternalPreprocessorLexemes, WistContributionIds.InternalPreprocessorLexemesModule),
        Feature(WistFeatureIds.Labels, WistContributionIds.LabelsModule),
        Feature(WistFeatureIds.Loops, WistContributionIds.LoopsModule),
        Feature(WistFeatureIds.NativeTypes, WistContributionIds.NativeTypesModule),
        Feature(WistFeatureIds.ParametersSetter, WistContributionIds.ParametersSetterModule),
        Feature(WistFeatureIds.SafeMathFunctions, WistContributionIds.SafeMathFunctionsModule),
        Feature(WistFeatureIds.SemicolonAsNewLine, WistContributionIds.SemicolonAsNewLineModule),
        Feature(WistFeatureIds.TextualAddition, WistContributionIds.TextualAdditionModule),
        Feature(WistFeatureIds.ArithmeticOptimization, WistContributionIds.ArithmeticOptimizer),
        Feature(WistFeatureIds.BooleanOptimization, WistContributionIds.BooleanOptimizer),
        Feature(WistFeatureIds.ComparisonIntrinsicOptimization, WistContributionIds.ComparisonIntrinsicOptimizer),
        Feature(WistFeatureIds.EGraphOptimization, WistContributionIds.EGraphOptimizer),
        Feature(WistFeatureIds.NativeCilOptimization, WistContributionIds.NativeCilOptimizer),
        Feature(WistFeatureIds.NativeTypesOptimization, WistContributionIds.NativeTypesOptimizer),
        Feature(WistFeatureIds.SsaOptimization, WistContributionIds.SsaOptimizer),
        PolicyFeature(WistInternalFeatureIds.TrustedSecurity, WistInternalFeatureIds.RestrictedSecurity),
        PolicyFeature(WistInternalFeatureIds.RestrictedSecurity, WistInternalFeatureIds.TrustedSecurity),
        PolicyFeature(WistInternalFeatureIds.CompositionRestricted)
    ];

    private static IReadOnlyList<LanguageContributionDescriptor> CreateContributions()
    {
        var contributions = new List<LanguageContributionDescriptor>
        {
            new(
                WistContributionIds.Frontend,
                LanguageSlots.FrontendParser,
                LanguageSlotMultiplicity.Single,
                ContributionMergePolicy.RejectDuplicate,
                providesCapabilities: [new LanguageCapabilityId("frontend:wist")],
                transformation: new ArtifactTransformationDescriptor(
                    StandardLanguageArtifactKinds.SourceText.Contract,
                    WistArtifactKinds.SyntaxTreeContract,
                    10)),
            new(
                WistContributionIds.LoweringToBytecode,
                SyntaxToBytecodeSlot,
                LanguageSlotMultiplicity.Single,
                ContributionMergePolicy.RejectDuplicate,
                requiresCapabilities: [new LanguageCapabilityId("frontend:wist")],
                providesCapabilities: [new LanguageCapabilityId("lowering:bytecode")],
                transformation: new ArtifactTransformationDescriptor(
                    WistArtifactKinds.SyntaxTreeContract,
                    WistArtifactKinds.BytecodeContract,
                    10)),
            new(
                WistContributionIds.LoweringToAir,
                BytecodeToAirSlot,
                LanguageSlotMultiplicity.Single,
                ContributionMergePolicy.RejectDuplicate,
                requiresCapabilities: [new LanguageCapabilityId("lowering:bytecode")],
                providesCapabilities: [new LanguageCapabilityId("lowering:air")],
                transformation: new ArtifactTransformationDescriptor(
                    WistArtifactKinds.BytecodeContract,
                    WistArtifactKinds.AirContract,
                    10)),
            new(
                WistContributionIds.InterpreterBackend,
                LanguageSlots.Backends,
                providesCapabilities: [LanguageCapabilities.Backend(Interpreter)],
                supportedBackends: [Interpreter],
                transformation: new ArtifactTransformationDescriptor(
                    WistArtifactKinds.AirContract,
                    WistArtifactKinds.InterpreterArtifactContract,
                    10),
                backendInputContract: WistArtifactKinds.InterpreterArtifactContract),
            new(
                WistContributionIds.CilBackend,
                LanguageSlots.Backends,
                providesCapabilities: [LanguageCapabilities.Backend(Cil)],
                supportedBackends: [Cil],
                transformation: new ArtifactTransformationDescriptor(
                    WistArtifactKinds.AirContract,
                    WistArtifactKinds.CilArtifactContract,
                    10),
                backendInputContract: WistArtifactKinds.CilArtifactContract),
            new(
                WistContributionIds.RuntimeProvider,
                LanguageSlots.RuntimeProvider,
                LanguageSlotMultiplicity.Single,
                ContributionMergePolicy.RejectDuplicate,
                providesCapabilities: [LanguageCapabilities.RuntimeProvider],
                runtimeProviderId: RuntimeProviderId,
                runtimeProviderVersion: PackageVersion,
                runtimeInputContracts: new Dictionary<BackendId, LanguageArtifactContract>
                {
                    [Interpreter] = WistArtifactKinds.InterpreterArtifactContract,
                    [Cil] = WistArtifactKinds.CilArtifactContract
                })
        };

        contributions.AddRange(WistRuntimeComponentCatalog.Modules.Select(Module));
        contributions.AddRange(WistRuntimeComponentCatalog.Optimizers.Select(Optimizer));
        return contributions;
    }

    private static LanguageFeatureDescriptor Feature(
        LanguageFeatureId id,
        LanguageContributionId contribution) => Feature(id, [contribution]);

    private static LanguageFeatureDescriptor Feature(
        LanguageFeatureId id,
        IEnumerable<LanguageContributionId> contributions) => new(
        id,
        requires: GetRequiredFeatures(id),
        supportedBackends: BothBackends,
        contributions: contributions);

    private static LanguageFeatureDescriptor PolicyFeature(
        LanguageFeatureId id,
        params LanguageFeatureId[] conflicts) => new(
        id,
        conflicts: conflicts,
        supportedBackends: BothBackends);

    private static IReadOnlyList<LanguageFeatureId> GetRequiredFeatures(LanguageFeatureId id)
    {
        if (id == WistFeatureIds.Arithmetic)
            return [WistFeatureIds.Numbers, WistFeatureIds.Scopes, WistFeatureIds.Whitespaces];
        if (id == WistFeatureIds.Identifiers)
            return [WistFeatureIds.Scopes, WistFeatureIds.Whitespaces];
        if (id == WistFeatureIds.Variables)
            return [WistFeatureIds.Identifiers, WistFeatureIds.Scopes, WistFeatureIds.Whitespaces];
        if (id == WistFeatureIds.Comparisons)
            return [WistFeatureIds.Numbers, WistFeatureIds.Scopes, WistFeatureIds.Whitespaces];
        if (id == WistFeatureIds.BooleanLogic || id == WistFeatureIds.ConditionalControlFlow)
            return [WistFeatureIds.Scopes, WistFeatureIds.Whitespaces];
        if (id == WistFeatureIds.Conditions)
            return
            [
                WistFeatureIds.Comparisons,
                WistFeatureIds.BooleanLogic,
                WistFeatureIds.ConditionalControlFlow,
                WistFeatureIds.Numbers,
                WistFeatureIds.Scopes,
                WistFeatureIds.Whitespaces
            ];
        if (id == WistFeatureIds.Loops)
            return [WistFeatureIds.Conditions, WistFeatureIds.Scopes, WistFeatureIds.Whitespaces];
        if (id == WistFeatureIds.Equality)
            return [WistFeatureIds.Scopes, WistFeatureIds.Whitespaces];
        if (id == WistFeatureIds.FunctionCalls || id == WistFeatureIds.CSharpInterop)
            return [WistFeatureIds.Identifiers, WistFeatureIds.Scopes, WistFeatureIds.Whitespaces];
        if (id == WistFeatureIds.NativeTypes)
            return [WistFeatureIds.Scopes, WistFeatureIds.Whitespaces];
        if (id == WistFeatureIds.SafeMathFunctions)
            return [WistFeatureIds.FunctionCalls];
        if (id == WistFeatureIds.TextualAddition)
            return [WistFeatureIds.Scopes, WistFeatureIds.Whitespaces];
        return [];
    }

    private static LanguageContributionDescriptor Module(WistRuntimeComponentDescriptor component) => new(
        component.ContributionId,
        LanguageSlots.FrontendSyntax,
        requiresCapabilities: [new LanguageCapabilityId("frontend:wist"), new LanguageCapabilityId("lowering:air")],
        supportedBackends: BothBackends,
        order: component.Order,
        metadata: new Dictionary<string, string> { ["wist.moduleAlias"] = component.Alias });

    private static LanguageContributionDescriptor Optimizer(WistRuntimeComponentDescriptor component) => new(
        component.ContributionId,
        LanguageSlots.Optimizers,
        requiresCapabilities: [new LanguageCapabilityId("lowering:air")],
        supportedBackends: BothBackends,
        order: component.Order,
        metadata: new Dictionary<string, string> { ["wist.optimizerAlias"] = component.Alias });
}
