using BasicCore.Contracts;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.Testing.Infrastructure;

/// <summary>
/// Test/research-only package that inserts one exact AIR optimizer contribution into a planned Wist route.
/// The optimizer is selected by LanguageCompiler like any other contribution; no runtime discovery is used.
/// </summary>
public sealed class WistAirOptimizerTestPackage : ILanguageExtensionPackage, ILanguageRouteComponentSource
{
    private static readonly BackendId[] BothBackends = [new("cil"), new("interpreter")];
    private readonly Func<IAirOptimizer> _optimizerFactory;

    public WistAirOptimizerTestPackage(
        LanguagePackageId packageId,
        LanguageVersion packageVersion,
        LanguageFeatureId featureId,
        LanguageContributionId contributionId,
        Func<IAirOptimizer> optimizerFactory,
        int order = 10_000,
        LanguageRuntimeComponentTraits? traits = null)
    {
        _optimizerFactory = optimizerFactory ?? throw new ArgumentNullException(nameof(optimizerFactory));
        var componentTraits = traits ?? LanguageRuntimeComponentTraits.Unknown;
        FeatureId = featureId;
        ContributionId = contributionId;
        Descriptor = new LanguagePackageDescriptor(
            packageId,
            packageVersion,
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(featureId, contributions: [contributionId])],
            contributions:
            [
                new LanguageContributionDescriptor(
                    contributionId,
                    LanguageSlots.Optimizers,
                    requiresCapabilities: [new LanguageCapabilityId("lowering:air")],
                    supportedBackends: BothBackends,
                    transformation: new ArtifactTransformationDescriptor(
                        WistArtifactKinds.AirContract,
                        WistArtifactKinds.AirContract,
                        10),
                    order: order)
            ]);

        Components = new LanguageRouteComponentRegistry()
            .AddTransformer(LanguageTransformerRegistration.Create<WistAirArtifact, WistAirArtifact>(
                contributionId,
                WistDirectArtifactKinds.Air,
                WistDirectArtifactKinds.Air,
                componentTraits,
                _ => new OptimizerTransformer(contributionId, optimizerFactory, componentTraits)))
            .CreateCatalog();
    }

    public LanguageFeatureId FeatureId { get; }
    public LanguageContributionId ContributionId { get; }
    public LanguagePackageDescriptor Descriptor { get; }
    public LanguageRouteComponentCatalog Components { get; }

    private sealed class OptimizerTransformer(
        LanguageContributionId contributionId,
        Func<IAirOptimizer> optimizerFactory,
        LanguageRuntimeComponentTraits traits) : ILanguageArtifactTransformer<WistAirArtifact, WistAirArtifact>
    {
        public LanguageContributionId ContributionId { get; } = contributionId;
        public LanguageArtifactKind<WistAirArtifact> TypedSourceKind => WistDirectArtifactKinds.Air;
        public LanguageArtifactKind<WistAirArtifact> TypedTargetKind => WistDirectArtifactKinds.Air;
        public LanguageRuntimeComponentTraits TypedTraits { get; } = traits ?? throw new ArgumentNullException(nameof(traits));

        public WistAirArtifact Transform(WistAirArtifact source, LanguageArtifactTransformationContext context)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(context);
            var optimizer = optimizerFactory()
                ?? throw new InvalidOperationException(
                    $"Test optimizer factory for '{ContributionId.Value}' returned null.");
            var result = optimizer.Optimize(source.Air)
                ?? throw new InvalidOperationException(
                    $"Test optimizer '{ContributionId.Value}' returned null AIR.");
            return new WistAirArtifact(
                source.Input,
                result,
                source.Modules,
                source.Optimizers.Append(optimizer).ToArray(),
                source.Observation,
                source.SsaReport);
        }
    }
}
