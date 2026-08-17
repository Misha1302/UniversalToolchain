using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.EndToEndExperiments;

internal sealed class Cgo27FaultLanguagePackage : ILanguageExtensionPackage, ILanguageRouteComponentSource
{
    public static LanguageFeatureId FeatureId { get; } = new("cgo27.fault-injection");
    public static LanguageContributionId ContributionId { get; } = new("cgo27.optimizer.fault");
    public static LanguagePackageId PackageId { get; } = new("CGO27.EndToEnd.FaultPackage");
    public static LanguageVersion PackageVersion { get; } = new("1.0.0");

    private readonly LanguageRouteComponentCatalog _components;

    public Cgo27FaultLanguagePackage()
    {
        _components = new LanguageRouteComponentRegistry()
            .AddTransformer(LanguageTransformerRegistration.Create<WistAirArtifact, WistAirArtifact>(
                ContributionId,
                WistDirectArtifactKinds.Air,
                WistDirectArtifactKinds.Air,
                LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                _ => new FaultTransformer()))
            .CreateCatalog();
    }

    public LanguagePackageDescriptor Descriptor { get; } = new(
        PackageId,
        PackageVersion,
        ToolchainApi.Current,
        [new LanguageFeatureDescriptor(FeatureId, contributions: [ContributionId])],
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["purpose"] = "model-authored-cgo27-fault-injection"
        },
        [
            new LanguageContributionDescriptor(
                ContributionId,
                LanguageSlots.Optimizers,
                transformation: new ArtifactTransformationDescriptor(
                    WistArtifactKinds.AirContract,
                    WistArtifactKinds.AirContract),
                order: 10000)
        ]);

    LanguageRouteComponentCatalog ILanguageRouteComponentSource.Components => _components;

    public static LanguageDefinition AddFaultFeature(LanguageDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new LanguageDefinition(
            definition.Id,
            definition.Version,
            definition.ToolchainApiVersion,
            definition.SelectedFeatures.Append(FeatureId),
            definition.Backends,
            definition.RuntimeProvider,
            definition.RuntimePolicy,
            definition.Metadata,
            definition.SlotOverrides,
            definition.CapabilityProviders,
            definition.ExcludedContributions,
            definition.EntryArtifact,
            definition.ContributionOrderConstraints,
            definition.IntrinsicPolicy);
    }

    private sealed class FaultTransformer : ILanguageArtifactTransformer<WistAirArtifact, WistAirArtifact>
    {
        public LanguageContributionId ContributionId => Cgo27FaultLanguagePackage.ContributionId;
        public LanguageArtifactKind<WistAirArtifact> TypedSourceKind => WistDirectArtifactKinds.Air;
        public LanguageArtifactKind<WistAirArtifact> TypedTargetKind => WistDirectArtifactKinds.Air;
        public LanguageRuntimeComponentTraits TypedTraits => LanguageRuntimeComponentTraits.DeterministicNoHostInterop;

        public WistAirArtifact Transform(WistAirArtifact source, LanguageArtifactTransformationContext context)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(context);
            var optimizer = new Cgo27FaultOptimizer();
            var result = optimizer.Optimize(source.Air)
                ?? throw new InvalidOperationException("CGO27 fault optimizer returned null AIR.");
            var contractSnapshot = WistOptimizerContractSnapshot.Capture(ContributionId, optimizer);
            return new WistAirArtifact(
                source.Input,
                result,
                source.SsaReport,
                source.AppliedOptimizerContracts.Append(contractSnapshot).ToArray());
        }
    }
}
