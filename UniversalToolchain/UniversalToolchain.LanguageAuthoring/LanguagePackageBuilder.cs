using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageAuthoring;

/// <summary>
/// Builds package descriptors and immutable runtime component registrations from one model.
/// Components are created per runtime session unless an explicitly stateless singleton lifetime is requested.
/// </summary>
public sealed class LanguagePackageBuilder
{
    private readonly LanguagePackageId _packageId;
    private readonly LanguageVersion _version;
    private readonly List<FeatureDraft> _features = [];
    private readonly List<LanguageContributionDescriptor> _contributions = [];
    private readonly LanguageRouteComponentRegistry _components = new();
    private readonly SortedDictionary<string, string> _metadata = new(StringComparer.Ordinal);
    private ToolchainApiVersion _toolchainApi = ToolchainApi.Current;
    private LanguageRuntimeProviderReference? _runtimeProvider;
    private LanguageContributionId? _runtimeContributionId;

    private LanguagePackageBuilder(LanguagePackageId packageId, LanguageVersion version)
    {
        _packageId = packageId;
        _version = version;
    }

    public static LanguagePackageBuilder Create(string packageId, string version) =>
        new(new LanguagePackageId(packageId), new LanguageVersion(version));

    public LanguagePackageBuilder TargetToolchainApi(int major)
    {
        _toolchainApi = new ToolchainApiVersion(major);
        return this;
    }

    public LanguagePackageBuilder WithMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key must not be empty.", nameof(key));
        _metadata[key] = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    public LanguagePackageBuilder AddFeature(string featureId, Action<LanguageFeatureBuilder> configure) =>
        AddFeature(new LanguageFeatureId(featureId), configure);

    public LanguagePackageBuilder AddFeature(LanguageFeatureId featureId, Action<LanguageFeatureBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (_features.Any(feature => feature.Id == featureId))
            throw new InvalidOperationException($"Feature '{featureId.Value}' is already declared.");
        var draft = new FeatureDraft(featureId);
        _features.Add(draft);
        configure(new LanguageFeatureBuilder(this, draft));
        return this;
    }

    public LanguagePackageBuilder AddTransformer<TSource, TTarget>(
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
        EnsureStaticDelegate(transform, nameof(transform));
        return AddTransformerFactory(
            contributionId,
            slot,
            source,
            target,
            _ => new DelegateLanguageArtifactTransformer<TSource, TTarget>(
                new LanguageContributionId(contributionId), source, target, transform, traits),
            traits,
            cost,
            order,
            supportedBackends,
            mergePolicy,
            multiplicity,
            configure);
    }

    public LanguagePackageBuilder AddTransformerFactory<TSource, TTarget>(
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
        RegisterTransformerFactory(
            null,
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

    public LanguagePackageBuilder AddPass<TArtifact>(
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

    public LanguagePackageBuilder AddPassFactory<TArtifact>(
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

    public LanguagePackageBuilder AddBackend<TInput, TResult>(
        string backendId,
        string contributionId,
        LanguageArtifactKind<TInput> input,
        Func<TInput, LanguageArtifactTransformationContext, TResult> execute,
        LanguageRuntimeComponentTraits traits,
        int order = 0,
        Action<LanguageContributionBuilder>? configure = null)
    {
        EnsureStaticDelegate(execute, nameof(execute));
        return AddBackendFactory(
            backendId,
            contributionId,
            input,
            _ => new DelegateLanguageArtifactExecutor<TInput, TResult>(
                new LanguageContributionId(contributionId), new BackendId(backendId), input, execute, traits),
            traits,
            order,
            configure);
    }

    public LanguagePackageBuilder AddBackendFactory<TInput, TResult>(
        string backendId,
        string contributionId,
        LanguageArtifactKind<TInput> input,
        Func<LanguageRuntimeComponentContext, ILanguageArtifactExecutor<TInput, TResult>> factory,
        LanguageRuntimeComponentTraits traits,
        int order = 0,
        Action<LanguageContributionBuilder>? configure = null,
        LanguageRuntimeComponentLifetime lifetime = LanguageRuntimeComponentLifetime.PerSession)
    {
        RegisterBackendFactory(
            null,
            new BackendId(backendId),
            new LanguageContributionId(contributionId),
            input,
            factory,
            traits,
            order,
            configure,
            lifetime);
        return this;
    }

    public LanguagePackageBuilder UseRouteRuntime(string providerId, string version, string? contributionId = null) =>
        UseRouteRuntime(
            new LanguageRuntimeProviderId(providerId),
            new LanguageVersion(version),
            new LanguageContributionId(contributionId ?? providerId + ".runtime"));

    public LanguagePackageBuilder UseRouteRuntime(
        LanguageRuntimeProviderId providerId,
        LanguageVersion version,
        LanguageContributionId runtimeContributionId)
    {
        if (_runtimeProvider != null)
            throw new InvalidOperationException("A route runtime provider is already configured.");
        EnsureContributionIsUnique(runtimeContributionId);
        _runtimeProvider = new LanguageRuntimeProviderReference(providerId, version);
        _runtimeContributionId = runtimeContributionId;
        return this;
    }

    public AuthoredLanguagePackage Build()
    {
        if (_features.Count == 0 && _contributions.Count == 0 && _runtimeProvider == null)
            throw new InvalidOperationException("A language package must declare at least one feature, contribution, or runtime provider.");

        var contributions = _contributions.ToList();
        if (_runtimeProvider != null && _runtimeContributionId != null)
        {
            contributions.Add(new LanguageContributionDescriptor(
                _runtimeContributionId.Value,
                LanguageSlots.RuntimeProvider,
                LanguageSlotMultiplicity.Single,
                ContributionMergePolicy.RejectDuplicate,
                providesCapabilities: [LanguageCapabilities.RuntimeProvider],
                runtimeProviderId: _runtimeProvider.ProviderId,
                runtimeProviderVersion: _runtimeProvider.Version));
        }

        var descriptor = new LanguagePackageDescriptor(
            _packageId,
            _version,
            _toolchainApi,
            _features.Select(static feature => feature.ToDescriptor()),
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(_metadata, StringComparer.Ordinal)),
            contributions);
        return new AuthoredLanguagePackage(
            descriptor,
            _runtimeProvider,
            _runtimeContributionId,
            _components.CreateCatalog());
    }

    internal void RegisterTransformer<TSource, TTarget>(
        FeatureDraft? feature,
        LanguageContributionId contributionId,
        LanguageSlotId slot,
        LanguageArtifactKind<TSource> source,
        LanguageArtifactKind<TTarget> target,
        Func<TSource, LanguageArtifactTransformationContext, TTarget> transform,
        LanguageRuntimeComponentTraits traits,
        int cost,
        int order,
        IEnumerable<BackendId>? supportedBackends,
        ContributionMergePolicy mergePolicy,
        LanguageSlotMultiplicity multiplicity,
        Action<LanguageContributionBuilder>? configure)
    {
        EnsureStaticDelegate(transform, nameof(transform));
        RegisterTransformerFactory(
            feature,
            contributionId,
            slot,
            source,
            target,
            _ => new DelegateLanguageArtifactTransformer<TSource, TTarget>(
                contributionId, source, target, transform, traits),
            traits,
            cost,
            order,
            supportedBackends,
            mergePolicy,
            multiplicity,
            configure,
            LanguageRuntimeComponentLifetime.PerSession);
    }

    internal void RegisterTransformerFactory<TSource, TTarget>(
        FeatureDraft? feature,
        LanguageContributionId contributionId,
        LanguageSlotId slot,
        LanguageArtifactKind<TSource> source,
        LanguageArtifactKind<TTarget> target,
        Func<LanguageRuntimeComponentContext, ILanguageArtifactTransformer<TSource, TTarget>> factory,
        LanguageRuntimeComponentTraits traits,
        int cost,
        int order,
        IEnumerable<BackendId>? supportedBackends,
        ContributionMergePolicy mergePolicy,
        LanguageSlotMultiplicity multiplicity,
        Action<LanguageContributionBuilder>? configure,
        LanguageRuntimeComponentLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(factory);
        EnsureContributionIsUnique(contributionId);
        var options = BuildContributionOptions(configure);
        _contributions.Add(new LanguageContributionDescriptor(
            contributionId,
            slot,
            multiplicity,
            mergePolicy,
            options.RequiresContributionIds,
            options.ProvidedCapabilities,
            options.RequiredCapabilities,
            options.ConflictingContributions,
            options.ConflictingCapabilities,
            supportedBackends,
            ArtifactTransformationDescriptor.Create(source, target, cost),
            order: order,
            metadata: options.Metadata,
            beforeContributions: options.BeforeContributionIds,
            afterContributions: options.AfterContributionIds));
        feature?.Contributions.Add(contributionId);
        _components.AddTransformer(LanguageTransformerRegistration.Create(
            contributionId,
            source,
            target,
            traits,
            factory,
            lifetime));
    }

    internal void RegisterBackend<TInput, TResult>(
        FeatureDraft? feature,
        BackendId backend,
        LanguageContributionId contributionId,
        LanguageArtifactKind<TInput> input,
        Func<TInput, LanguageArtifactTransformationContext, TResult> execute,
        LanguageRuntimeComponentTraits traits,
        int order,
        Action<LanguageContributionBuilder>? configure)
    {
        EnsureStaticDelegate(execute, nameof(execute));
        RegisterBackendFactory(
            feature,
            backend,
            contributionId,
            input,
            _ => new DelegateLanguageArtifactExecutor<TInput, TResult>(
                contributionId, backend, input, execute, traits),
            traits,
            order,
            configure,
            LanguageRuntimeComponentLifetime.PerSession);
    }

    internal void RegisterBackendFactory<TInput, TResult>(
        FeatureDraft? feature,
        BackendId backend,
        LanguageContributionId contributionId,
        LanguageArtifactKind<TInput> input,
        Func<LanguageRuntimeComponentContext, ILanguageArtifactExecutor<TInput, TResult>> factory,
        LanguageRuntimeComponentTraits traits,
        int order,
        Action<LanguageContributionBuilder>? configure,
        LanguageRuntimeComponentLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(factory);
        EnsureContributionIsUnique(contributionId);
        var options = BuildContributionOptions(configure);
        _contributions.Add(new LanguageContributionDescriptor(
            contributionId,
            LanguageSlots.Backends,
            providesCapabilities: options.ProvidedCapabilities.Append(LanguageCapabilities.Backend(backend)),
            requiresContributions: options.RequiresContributionIds,
            requiresCapabilities: options.RequiredCapabilities,
            conflicts: options.ConflictingContributions,
            conflictsCapabilities: options.ConflictingCapabilities,
            supportedBackends: [backend],
            order: order,
            metadata: options.Metadata,
            backendInputContract: input.Contract,
            beforeContributions: options.BeforeContributionIds,
            afterContributions: options.AfterContributionIds));
        feature?.Contributions.Add(contributionId);
        _components.AddExecutor(LanguageExecutorRegistration.Create(
            contributionId,
            backend,
            input,
            traits,
            factory,
            lifetime));
    }

    private static LanguageContributionBuilder BuildContributionOptions(Action<LanguageContributionBuilder>? configure)
    {
        var builder = new LanguageContributionBuilder();
        configure?.Invoke(builder);
        return builder;
    }

    private static void EnsureStaticDelegate(Delegate callback, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(callback, parameterName);
        if (callback.Target == null)
            return;

        // Roslyn may cache a non-capturing lambda on a compiler-generated singleton (<>c),
        // so Delegate.Target alone cannot distinguish a closure from a stateless lambda.
        // A genuine capture is represented by a compiler-generated target with instance fields.
        var targetType = callback.Target.GetType();
        var isCompilerGenerated = targetType.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
        var hasInstanceState = targetType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length != 0;
        if (isCompilerGenerated && !hasInstanceState)
            return;

        throw new ArgumentException(
            "Captured delegates can share mutable closure state between runtimes. Use the corresponding component-factory overload for stateful components.",
            parameterName);
    }

    private void EnsureContributionIsUnique(LanguageContributionId contributionId)
    {
        if (_contributions.Any(contribution => contribution.Id == contributionId) || _runtimeContributionId == contributionId)
            throw new InvalidOperationException($"Contribution '{contributionId.Value}' is already declared.");
    }

    internal sealed class FeatureDraft(LanguageFeatureId id)
    {
        public LanguageFeatureId Id { get; } = id;
        public List<LanguageFeatureId> Requires { get; } = [];
        public List<LanguageFeatureId> Conflicts { get; } = [];
        public List<BackendId> SupportedBackends { get; } = [];
        public List<LanguageContributionId> Contributions { get; } = [];
        public SortedDictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);

        public LanguageFeatureDescriptor ToDescriptor() => new(
            Id,
            Requires,
            Conflicts,
            SupportedBackends,
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(Metadata, StringComparer.Ordinal)),
            Contributions);
    }
}
