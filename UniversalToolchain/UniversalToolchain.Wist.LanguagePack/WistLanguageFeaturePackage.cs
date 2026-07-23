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
    public static LanguageFeatureId Conditions { get; } = new("wist.conditions");
}

public static class WistContributionIds
{
    public static LanguageContributionId Frontend { get; } = new("wist.frontend.parser");
    public static LanguageContributionId LoweringToAir { get; } = new("wist.lowering.air");
    public static LanguageContributionId InterpreterBackend { get; } = new("wist.backend.interpreter");
    public static LanguageContributionId CilBackend { get; } = new("wist.backend.cil");
    public static LanguageContributionId LegacyRuntimeAdapter { get; } = new("wist.runtime.legacy-adapter");
    public static LanguageContributionId WhitespacesModule { get; } = new("wist.module.whitespaces");
    public static LanguageContributionId ScopesModule { get; } = new("wist.module.scopes");
    public static LanguageContributionId NumbersModule { get; } = new("wist.module.numbers");
    public static LanguageContributionId ArithmeticModule { get; } = new("wist.module.arithmetic");
    public static LanguageContributionId IdentifiersModule { get; } = new("wist.module.identifiers");
    public static LanguageContributionId VariablesModule { get; } = new("wist.module.variables");
    public static LanguageContributionId ConditionsModule { get; } = new("wist.module.conditions");
}

public static class WistArtifactKinds
{
    public static LanguageArtifactKindId SyntaxTree { get; } = new("wist.syntax-tree");
    public static LanguageArtifactKindId InterpreterArtifact { get; } = new("wist.interpreter.artifact");
    public static LanguageArtifactKindId CilArtifact { get; } = new("wist.cil-artifact");

    // Compatibility artifacts are not materialized by the generic route runtime, but they still
    // require explicit protocol identities so wildcard contracts cannot enter a canonical plan.
    public static LanguageArtifactContract SyntaxTreeContract { get; } =
        new(SyntaxTree, "wist.syntax-tree/v1");
    public static LanguageArtifactContract AirContract { get; } =
        new(LanguageArtifacts.Air, "wist.air/v1");
    public static LanguageArtifactContract InterpreterArtifactContract { get; } =
        new(InterpreterArtifact, "wist.interpreter-artifact/v1");
    public static LanguageArtifactContract CilArtifactContract { get; } =
        new(CilArtifact, "wist.cil-artifact/v1");
}

public sealed class WistLanguageFeaturePackage : ILanguageExtensionPackage
{
    public static LanguagePackageId PackageId { get; } = new("UniversalToolchain.Wist.LanguagePack");
    public static LanguageVersion PackageVersion { get; } = new(WistLanguagePackIdentity.Version);
    public static LanguageRuntimeProviderId RuntimeProviderId { get; } = new(PackageId.Value);

    public LanguagePackageDescriptor Descriptor { get; } = new(
        PackageId,
        PackageVersion,
        ToolchainApi.Current,
        [
            Feature(WistFeatureIds.Whitespaces, WistContributionIds.WhitespacesModule),
            Feature(WistFeatureIds.Scopes, WistContributionIds.ScopesModule, [WistFeatureIds.Whitespaces]),
            Feature(WistFeatureIds.Numbers, WistContributionIds.NumbersModule, [WistFeatureIds.Scopes]),
            Feature(WistFeatureIds.Arithmetic, WistContributionIds.ArithmeticModule, [WistFeatureIds.Numbers]),
            Feature(WistFeatureIds.Identifiers, WistContributionIds.IdentifiersModule, [WistFeatureIds.Scopes]),
            Feature(WistFeatureIds.Variables, WistContributionIds.VariablesModule, [WistFeatureIds.Identifiers, WistFeatureIds.Numbers]),
            Feature(WistFeatureIds.Conditions, WistContributionIds.ConditionsModule, [WistFeatureIds.Arithmetic])
        ],
        new Dictionary<string, string>
        {
            ["language"] = "Wist",
            ["status"] = "reference-contribution-pack-alpha"
        },
        [
            new LanguageContributionDescriptor(
                WistContributionIds.Frontend,
                LanguageSlots.FrontendParser,
                LanguageSlotMultiplicity.Single,
                ContributionMergePolicy.RejectDuplicate,
                providesCapabilities: [new LanguageCapabilityId("frontend:wist")],
                transformation: new ArtifactTransformationDescriptor(
                    StandardLanguageArtifactKinds.SourceText.Contract,
                    WistArtifactKinds.SyntaxTreeContract,
                    10)),
            new LanguageContributionDescriptor(
                WistContributionIds.LoweringToAir,
                LanguageSlots.Lowering,
                LanguageSlotMultiplicity.Single,
                ContributionMergePolicy.RejectDuplicate,
                requiresCapabilities: [new LanguageCapabilityId("frontend:wist")],
                providesCapabilities: [new LanguageCapabilityId("lowering:air")],
                transformation: new ArtifactTransformationDescriptor(
                    WistArtifactKinds.SyntaxTreeContract,
                    WistArtifactKinds.AirContract,
                    10)),
            new LanguageContributionDescriptor(
                WistContributionIds.InterpreterBackend,
                LanguageSlots.Backends,
                providesCapabilities: [LanguageCapabilities.Backend(new BackendId("interpreter"))],
                supportedBackends: [new BackendId("interpreter")],
                transformation: new ArtifactTransformationDescriptor(
                    WistArtifactKinds.AirContract,
                    WistArtifactKinds.InterpreterArtifactContract,
                    10),
                backendInputContract: WistArtifactKinds.InterpreterArtifactContract),
            new LanguageContributionDescriptor(
                WistContributionIds.CilBackend,
                LanguageSlots.Backends,
                providesCapabilities: [LanguageCapabilities.Backend(new BackendId("cil"))],
                supportedBackends: [new BackendId("cil")],
                transformation: new ArtifactTransformationDescriptor(
                    WistArtifactKinds.AirContract,
                    WistArtifactKinds.CilArtifactContract,
                    10),
                backendInputContract: WistArtifactKinds.CilArtifactContract),
            new LanguageContributionDescriptor(
                WistContributionIds.LegacyRuntimeAdapter,
                LanguageSlots.RuntimeProvider,
                LanguageSlotMultiplicity.Single,
                ContributionMergePolicy.RejectDuplicate,
                providesCapabilities: [LanguageCapabilities.RuntimeProvider],
                runtimeProviderId: RuntimeProviderId,
                runtimeProviderVersion: PackageVersion,
                runtimeInputContracts: new Dictionary<BackendId, LanguageArtifactContract>
                {
                    [new BackendId("interpreter")] = WistArtifactKinds.InterpreterArtifactContract,
                    [new BackendId("cil")] = WistArtifactKinds.CilArtifactContract
                },
                metadata: new Dictionary<string, string> { ["adapter"] = "legacy-wist-dialect" }),
            Module(WistContributionIds.WhitespacesModule, "Whitespaces"),
            Module(WistContributionIds.ScopesModule, "Scopes"),
            Module(WistContributionIds.NumbersModule, "Numbers"),
            Module(WistContributionIds.ArithmeticModule, "Arithmetic"),
            Module(WistContributionIds.IdentifiersModule, "Identifier"),
            Module(WistContributionIds.VariablesModule, "Variables"),
            Module(WistContributionIds.ConditionsModule, "ComparisonConditions")
        ]);

    private static LanguageFeatureDescriptor Feature(
        LanguageFeatureId id,
        LanguageContributionId contribution,
        IEnumerable<LanguageFeatureId>? requires = null) => new(
            id,
            requires,
            supportedBackends: [new BackendId("cil"), new BackendId("interpreter")],
            contributions: [contribution]);

    private static LanguageContributionDescriptor Module(LanguageContributionId id, string moduleAlias) => new(
        id,
        LanguageSlots.FrontendSyntax,
        requiresCapabilities: [new LanguageCapabilityId("frontend:wist"), new LanguageCapabilityId("lowering:air")],
        supportedBackends: [new BackendId("cil"), new BackendId("interpreter")],
        metadata: new Dictionary<string, string> { ["wist.moduleAlias"] = moduleAlias });
}
