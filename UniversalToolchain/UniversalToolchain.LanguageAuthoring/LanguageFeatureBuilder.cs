using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageAuthoring;

public sealed class LanguageFeatureBuilder
{
    private readonly LanguagePackageBuilder _owner;
    private readonly LanguagePackageBuilder.FeatureDraft _feature;

    internal LanguageFeatureBuilder(LanguagePackageBuilder owner, LanguagePackageBuilder.FeatureDraft feature)
    {
        _owner = owner;
        _feature = feature;
    }

    public LanguageFeatureBuilder Requires(params LanguageFeatureId[] features)
    {
        _feature.Requires.AddRange(features ?? throw new ArgumentNullException(nameof(features)));
        return this;
    }

    public LanguageFeatureBuilder ConflictsWith(params LanguageFeatureId[] features)
    {
        _feature.Conflicts.AddRange(features ?? throw new ArgumentNullException(nameof(features)));
        return this;
    }

    public LanguageFeatureBuilder SupportsBackends(params BackendId[] backends)
    {
        _feature.SupportedBackends.AddRange(backends ?? throw new ArgumentNullException(nameof(backends)));
        return this;
    }

    public LanguageFeatureBuilder WithMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key must not be empty.", nameof(key));
        _feature.Metadata[key] = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    public LanguageFeatureBuilder AddTransformer<TSource, TTarget>(
        string contributionId,
        LanguageSlotId slot,
        LanguageArtifactKind<TSource> source,
        LanguageArtifactKind<TTarget> target,
        Func<TSource, LanguageArtifactTransformationContext, TTarget> transform,
        LanguageRuntimeComponentTraits traits,
        int cost = 100,
        int order = 0,
        IEnumerable<BackendId>? supportedBackends = null,
        ContributionMergePolicy mergePolicy = ContributionMergePolicy.Add,
        LanguageSlotMultiplicity multiplicity = LanguageSlotMultiplicity.Many,
        Action<LanguageContributionBuilder>? configure = null)
    {
        _owner.RegisterTransformer(
            _feature,
            new LanguageContributionId(contributionId),
            slot,
            source,
            target,
            transform,
            traits,
            cost,
            order,
            supportedBackends,
            mergePolicy,
            multiplicity,
            configure);
        return this;
    }

    public LanguageFeatureBuilder AddTransformerFactory<TSource, TTarget>(
        string contributionId,
        LanguageSlotId slot,
        LanguageArtifactKind<TSource> source,
        LanguageArtifactKind<TTarget> target,
        Func<LanguageRuntimeComponentContext, ILanguageArtifactTransformer<TSource, TTarget>> factory,
        LanguageRuntimeComponentTraits traits,
        int cost = 100,
        int order = 0,
        IEnumerable<BackendId>? supportedBackends = null,
        ContributionMergePolicy mergePolicy = ContributionMergePolicy.Add,
        LanguageSlotMultiplicity multiplicity = LanguageSlotMultiplicity.Many,
        Action<LanguageContributionBuilder>? configure = null,
        LanguageRuntimeComponentLifetime lifetime = LanguageRuntimeComponentLifetime.PerSession)
    {
        _owner.RegisterTransformerFactory(
            _feature,
            new LanguageContributionId(contributionId),
            slot,
            source,
            target,
            factory,
            traits,
            cost,
            order,
            supportedBackends,
            mergePolicy,
            multiplicity,
            configure,
            lifetime);
        return this;
    }

    public LanguageFeatureBuilder AddPass<TArtifact>(
        string contributionId,
        LanguageSlotId slot,
        LanguageArtifactKind<TArtifact> artifact,
        Func<TArtifact, LanguageArtifactTransformationContext, TArtifact> transform,
        LanguageRuntimeComponentTraits traits,
        int order = 0,
        IEnumerable<BackendId>? supportedBackends = null,
        Action<LanguageContributionBuilder>? configure = null) =>
        AddTransformer(
            contributionId,
            slot,
            artifact,
            artifact,
            transform,
            traits,
            cost: 0,
            order,
            supportedBackends,
            ContributionMergePolicy.Decorate,
            LanguageSlotMultiplicity.Many,
            configure);

    public LanguageFeatureBuilder AddPassFactory<TArtifact>(
        string contributionId,
        LanguageSlotId slot,
        LanguageArtifactKind<TArtifact> artifact,
        Func<LanguageRuntimeComponentContext, ILanguageArtifactTransformer<TArtifact, TArtifact>> factory,
        LanguageRuntimeComponentTraits traits,
        int order = 0,
        IEnumerable<BackendId>? supportedBackends = null,
        Action<LanguageContributionBuilder>? configure = null,
        LanguageRuntimeComponentLifetime lifetime = LanguageRuntimeComponentLifetime.PerSession) =>
        AddTransformerFactory(
            contributionId,
            slot,
            artifact,
            artifact,
            factory,
            traits,
            cost: 0,
            order,
            supportedBackends,
            ContributionMergePolicy.Decorate,
            LanguageSlotMultiplicity.Many,
            configure,
            lifetime);

    public LanguageFeatureBuilder AddBackend<TInput, TResult>(
        string backendId,
        string contributionId,
        LanguageArtifactKind<TInput> input,
        Func<TInput, LanguageArtifactTransformationContext, TResult> execute,
        LanguageRuntimeComponentTraits traits,
        int order = 0,
        Action<LanguageContributionBuilder>? configure = null) =>
        AddBackend(
            new BackendId(backendId),
            new LanguageContributionId(contributionId),
            input,
            execute,
            traits,
            order,
            configure);

    public LanguageFeatureBuilder AddBackend<TInput, TResult>(
        BackendId backend,
        LanguageContributionId contributionId,
        LanguageArtifactKind<TInput> input,
        Func<TInput, LanguageArtifactTransformationContext, TResult> execute,
        LanguageRuntimeComponentTraits traits,
        int order = 0,
        Action<LanguageContributionBuilder>? configure = null)
    {
        _owner.RegisterBackend(
            _feature,
            backend,
            contributionId,
            input,
            execute,
            traits,
            order,
            configure);
        return this;
    }

    public LanguageFeatureBuilder AddBackendFactory<TInput, TResult>(
        BackendId backend,
        LanguageContributionId contributionId,
        LanguageArtifactKind<TInput> input,
        Func<LanguageRuntimeComponentContext, ILanguageArtifactExecutor<TInput, TResult>> factory,
        LanguageRuntimeComponentTraits traits,
        int order = 0,
        Action<LanguageContributionBuilder>? configure = null,
        LanguageRuntimeComponentLifetime lifetime = LanguageRuntimeComponentLifetime.PerSession)
    {
        _owner.RegisterBackendFactory(
            _feature,
            backend,
            contributionId,
            input,
            factory,
            traits,
            order,
            configure,
            lifetime);
        return this;
    }
}
