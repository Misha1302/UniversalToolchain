using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Runtime;


public enum LanguageComponentDeterminism
{
    Unknown,
    Deterministic,
    NonDeterministic
}

public enum LanguageComponentHostInterop
{
    Unknown,
    None,
    UsesHostInterop
}

public sealed record LanguageRuntimeComponentTraits(
    LanguageComponentDeterminism Determinism,
    LanguageComponentHostInterop HostInterop)
{
    public static LanguageRuntimeComponentTraits Unknown { get; } =
        new(LanguageComponentDeterminism.Unknown, LanguageComponentHostInterop.Unknown);
    public static LanguageRuntimeComponentTraits DeterministicNoHostInterop { get; } =
        new(LanguageComponentDeterminism.Deterministic, LanguageComponentHostInterop.None);
}

public interface ILanguageRuntimePolicyValidator
{
    void ValidatePolicy(LanguagePlan plan, LanguageRuntimePolicy policy, LanguageRuntimeOptions options);
}

public sealed class LanguageRuntimeOptions
{
    public LanguageRuntimeOptions(IEnumerable<Assembly>? allowedAssemblies = null)
    {
        AllowedAssemblies = new ReadOnlyCollection<Assembly>((allowedAssemblies ?? []).Distinct().ToList());
    }

    public IReadOnlyList<Assembly> AllowedAssemblies { get; }
}

public sealed class LanguageExecutionRequest
{
    public LanguageExecutionRequest(
        string source,
        BackendId backend,
        IReadOnlyDictionary<string, object?>? arguments = null)
        : this(new LanguageArtifact<string>(StandardLanguageArtifactKinds.SourceText, source), backend, arguments)
    {
        ArgumentNullException.ThrowIfNull(source);
    }

    public LanguageExecutionRequest(
        LanguageArtifact input,
        BackendId backend,
        IReadOnlyDictionary<string, object?>? arguments = null)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Backend = backend;
        Arguments = new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?>(arguments ?? new Dictionary<string, object?>(), StringComparer.Ordinal));
    }

    public LanguageArtifact Input { get; }
    public string Source => Input.GetRequiredValue<string>();
    public BackendId Backend { get; }
    public IReadOnlyDictionary<string, object?> Arguments { get; }

    public T GetRequiredInput<T>() => Input.GetRequiredValue<T>();

    public static LanguageExecutionRequest FromArtifact<T>(
        LanguageArtifactKind<T> kind,
        T value,
        BackendId backend,
        IReadOnlyDictionary<string, object?>? arguments = null) =>
        new(new LanguageArtifact<T>(kind, value), backend, arguments);
}

public sealed record LanguageExecutionResult(BackendId Backend, object? Value);

public abstract class LanguageArtifact
{
    protected LanguageArtifact(LanguageArtifactContract contract) => Contract = contract;
    protected LanguageArtifact(LanguageArtifactKindId kind) : this(new LanguageArtifactContract(kind))
    {
    }

    public LanguageArtifactContract Contract { get; }
    public LanguageArtifactKindId Kind => Contract.Kind;
    public abstract Type ValueType { get; }

    public T GetRequiredValue<T>()
    {
        if (this is LanguageArtifact<T> typed)
            return typed.Value;
        throw new InvalidOperationException(
            $"Artifact '{Contract}' contains CLR value type '{ValueType.FullName}', not '{typeof(T).FullName}'.");
    }
}

public sealed class LanguageArtifact<T> : LanguageArtifact
{
    public LanguageArtifact(LanguageArtifactKind<T> kind, T value)
        : base((kind ?? throw new ArgumentNullException(nameof(kind))).Contract) => Value = value;

    public T Value { get; }
    public override Type ValueType => typeof(T);
}

public sealed class LanguageArtifactTransformationContext
{
    public LanguageArtifactTransformationContext(
        LanguagePlan plan,
        LanguageExecutionRequest request,
        LanguageRuntimeOptions options)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        Request = request ?? throw new ArgumentNullException(nameof(request));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public LanguagePlan Plan { get; }
    public LanguageExecutionRequest Request { get; }
    public LanguageRuntimeOptions Options { get; }
}

public interface ILanguageArtifactTransformer
{
    LanguageContributionId ContributionId { get; }
    LanguageArtifactKindId SourceKind { get; }
    LanguageArtifactKindId TargetKind { get; }
    LanguageArtifactContract SourceContract => new(SourceKind);
    LanguageArtifactContract TargetContract => new(TargetKind);
    LanguageRuntimeComponentTraits Traits => LanguageRuntimeComponentTraits.Unknown;
    LanguageArtifact Transform(LanguageArtifact source, LanguageArtifactTransformationContext context);
}

public interface ILanguageArtifactTransformer<TSource, TTarget> : ILanguageArtifactTransformer
{
    LanguageArtifactKind<TSource> TypedSourceKind { get; }
    LanguageArtifactKind<TTarget> TypedTargetKind { get; }
    LanguageRuntimeComponentTraits TypedTraits { get; }
    TTarget Transform(TSource source, LanguageArtifactTransformationContext context);

    LanguageArtifactKindId ILanguageArtifactTransformer.SourceKind => TypedSourceKind.Id;
    LanguageArtifactKindId ILanguageArtifactTransformer.TargetKind => TypedTargetKind.Id;
    LanguageArtifactContract ILanguageArtifactTransformer.SourceContract => TypedSourceKind.Contract;
    LanguageArtifactContract ILanguageArtifactTransformer.TargetContract => TypedTargetKind.Contract;
    LanguageRuntimeComponentTraits ILanguageArtifactTransformer.Traits => TypedTraits;

    LanguageArtifact ILanguageArtifactTransformer.Transform(LanguageArtifact source, LanguageArtifactTransformationContext context) =>
        new LanguageArtifact<TTarget>(TypedTargetKind, Transform(source.GetRequiredValue<TSource>(), context));
}

public sealed class DelegateLanguageArtifactTransformer<TSource, TTarget> : ILanguageArtifactTransformer<TSource, TTarget>
{
    private readonly Func<TSource, LanguageArtifactTransformationContext, TTarget> _transform;

    public DelegateLanguageArtifactTransformer(
        LanguageContributionId contributionId,
        LanguageArtifactKind<TSource> source,
        LanguageArtifactKind<TTarget> target,
        Func<TSource, LanguageArtifactTransformationContext, TTarget> transform,
        LanguageRuntimeComponentTraits traits)
    {
        ContributionId = contributionId;
        TypedSourceKind = source ?? throw new ArgumentNullException(nameof(source));
        TypedTargetKind = target ?? throw new ArgumentNullException(nameof(target));
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
        TypedTraits = traits ?? throw new ArgumentNullException(nameof(traits));
    }

    public LanguageContributionId ContributionId { get; }
    public LanguageArtifactKind<TSource> TypedSourceKind { get; }
    public LanguageArtifactKind<TTarget> TypedTargetKind { get; }
    public LanguageRuntimeComponentTraits TypedTraits { get; }
    public TTarget Transform(TSource source, LanguageArtifactTransformationContext context) => _transform(source, context);
}

public interface ILanguageArtifactExecutor
{
    LanguageContributionId ContributionId { get; }
    BackendId Backend { get; }
    LanguageArtifactKindId InputKind { get; }
    LanguageArtifactContract InputContract => new(InputKind);
    LanguageRuntimeComponentTraits Traits => LanguageRuntimeComponentTraits.Unknown;
    object? Execute(LanguageArtifact artifact, LanguageArtifactTransformationContext context);
}

public interface ILanguageArtifactExecutor<TInput, TResult> : ILanguageArtifactExecutor
{
    LanguageArtifactKind<TInput> TypedInputKind { get; }
    LanguageRuntimeComponentTraits TypedTraits { get; }
    TResult Execute(TInput input, LanguageArtifactTransformationContext context);

    LanguageArtifactKindId ILanguageArtifactExecutor.InputKind => TypedInputKind.Id;
    LanguageArtifactContract ILanguageArtifactExecutor.InputContract => TypedInputKind.Contract;
    LanguageRuntimeComponentTraits ILanguageArtifactExecutor.Traits => TypedTraits;
    object? ILanguageArtifactExecutor.Execute(LanguageArtifact artifact, LanguageArtifactTransformationContext context) =>
        Execute(artifact.GetRequiredValue<TInput>(), context);
}

public sealed class DelegateLanguageArtifactExecutor<TInput, TResult> : ILanguageArtifactExecutor<TInput, TResult>
{
    private readonly Func<TInput, LanguageArtifactTransformationContext, TResult> _execute;

    public DelegateLanguageArtifactExecutor(
        LanguageContributionId contributionId,
        BackendId backend,
        LanguageArtifactKind<TInput> input,
        Func<TInput, LanguageArtifactTransformationContext, TResult> execute,
        LanguageRuntimeComponentTraits traits)
    {
        ContributionId = contributionId;
        Backend = backend;
        TypedInputKind = input ?? throw new ArgumentNullException(nameof(input));
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        TypedTraits = traits ?? throw new ArgumentNullException(nameof(traits));
    }

    public LanguageContributionId ContributionId { get; }
    public BackendId Backend { get; }
    public LanguageArtifactKind<TInput> TypedInputKind { get; }
    public LanguageRuntimeComponentTraits TypedTraits { get; }
    public TResult Execute(TInput input, LanguageArtifactTransformationContext context) => _execute(input, context);
}

public enum LanguageRuntimeComponentLifetime
{
    PerSession,
    SingletonStateless
}

public sealed class LanguageRuntimeComponentContext
{
    public LanguageRuntimeComponentContext(LanguagePlan plan, LanguageRuntimeOptions options)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public LanguagePlan Plan { get; }
    public LanguageRuntimeOptions Options { get; }
}

/// <summary>
/// Explicit opt-in contract for a component instance that is immutable, thread-safe and safe to
/// share between unrelated language runtimes. Shared components must not own disposable resources.
/// </summary>
public interface IStatelessLanguageRuntimeComponent
{
}

public sealed class LanguageTransformerRegistration
{
    private readonly Func<LanguageRuntimeComponentContext, ILanguageArtifactTransformer> _factory;
    private static readonly object CreatedMarker = new();
    private readonly object _gate = new();
    private readonly ConditionalWeakTable<ILanguageArtifactTransformer, object> _createdInstances = new();
    private ILanguageArtifactTransformer? _singleton;

    public LanguageTransformerRegistration(
        LanguageContributionId contributionId,
        LanguageArtifactContract sourceContract,
        LanguageArtifactContract targetContract,
        LanguageRuntimeComponentTraits traits,
        Func<LanguageRuntimeComponentContext, ILanguageArtifactTransformer> factory,
        LanguageRuntimeComponentLifetime lifetime = LanguageRuntimeComponentLifetime.PerSession)
    {
        ContributionId = contributionId;
        SourceContract = sourceContract;
        TargetContract = targetContract;
        Traits = traits ?? throw new ArgumentNullException(nameof(traits));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Lifetime = lifetime;
    }

    public LanguageContributionId ContributionId { get; }
    public LanguageArtifactContract SourceContract { get; }
    public LanguageArtifactContract TargetContract { get; }
    public LanguageRuntimeComponentTraits Traits { get; }
    public LanguageRuntimeComponentLifetime Lifetime { get; }
    internal bool IsOwnedBySession => Lifetime == LanguageRuntimeComponentLifetime.PerSession;

    public static LanguageTransformerRegistration Create<TSource, TTarget>(
        LanguageContributionId contributionId,
        LanguageArtifactKind<TSource> source,
        LanguageArtifactKind<TTarget> target,
        LanguageRuntimeComponentTraits traits,
        Func<LanguageRuntimeComponentContext, ILanguageArtifactTransformer<TSource, TTarget>> factory,
        LanguageRuntimeComponentLifetime lifetime = LanguageRuntimeComponentLifetime.PerSession)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(factory);
        return new LanguageTransformerRegistration(
            contributionId,
            source.Contract,
            target.Contract,
            traits,
            context => factory(context),
            lifetime);
    }

    public static LanguageTransformerRegistration FromStatelessSingleton(ILanguageArtifactTransformer transformer)
    {
        ArgumentNullException.ThrowIfNull(transformer);
        return new LanguageTransformerRegistration(
            transformer.ContributionId,
            transformer.SourceContract,
            transformer.TargetContract,
            transformer.Traits,
            _ => transformer,
            LanguageRuntimeComponentLifetime.SingletonStateless);
    }

    internal ILanguageArtifactTransformer Create(LanguageRuntimeComponentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Lifetime == LanguageRuntimeComponentLifetime.SingletonStateless)
        {
            lock (_gate)
            {
                _singleton ??= CreateAndValidate(context);
                return _singleton;
            }
        }

        var component = CreateAndValidate(context);
        if (!_createdInstances.TryAdd(component, CreatedMarker))
        {
            throw new InvalidOperationException(
                $"Transformer factory for contribution '{ContributionId.Value}' reused an instance across runtime sessions. " +
                "Return a fresh component or opt in to SingletonStateless with an explicitly stateless component.");
        }
        return component;
    }

    private ILanguageArtifactTransformer CreateAndValidate(LanguageRuntimeComponentContext context)
    {
        var component = _factory(context)
            ?? throw new InvalidOperationException($"Transformer factory for '{ContributionId.Value}' returned null.");
        if (component.ContributionId != ContributionId ||
            component.SourceContract != SourceContract ||
            component.TargetContract != TargetContract ||
            component.Traits != Traits)
        {
            throw new InvalidOperationException(
                $"Transformer factory for '{ContributionId.Value}' returned a component whose identity, contracts, or traits do not match its registration.");
        }
        if (Lifetime == LanguageRuntimeComponentLifetime.SingletonStateless)
            ValidateStatelessSingleton(component, "Transformer", ContributionId.Value);
        return component;
    }

    private static void ValidateStatelessSingleton(object component, string kind, string id)
    {
        if (component is not IStatelessLanguageRuntimeComponent)
        {
            throw new InvalidOperationException(
                $"{kind} '{id}' is registered as SingletonStateless but does not implement {nameof(IStatelessLanguageRuntimeComponent)}.");
        }
        if (component is IDisposable or IAsyncDisposable)
        {
            throw new InvalidOperationException(
                $"{kind} '{id}' is registered as SingletonStateless but owns disposable resources. Use PerSession lifetime instead.");
        }
    }
}

public sealed class LanguageExecutorRegistration
{
    private readonly Func<LanguageRuntimeComponentContext, ILanguageArtifactExecutor> _factory;
    private static readonly object CreatedMarker = new();
    private readonly object _gate = new();
    private readonly ConditionalWeakTable<ILanguageArtifactExecutor, object> _createdInstances = new();
    private ILanguageArtifactExecutor? _singleton;

    public LanguageExecutorRegistration(
        LanguageContributionId contributionId,
        BackendId backend,
        LanguageArtifactContract inputContract,
        LanguageRuntimeComponentTraits traits,
        Func<LanguageRuntimeComponentContext, ILanguageArtifactExecutor> factory,
        LanguageRuntimeComponentLifetime lifetime = LanguageRuntimeComponentLifetime.PerSession)
    {
        ContributionId = contributionId;
        Backend = backend;
        InputContract = inputContract;
        Traits = traits ?? throw new ArgumentNullException(nameof(traits));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Lifetime = lifetime;
    }

    public LanguageContributionId ContributionId { get; }
    public BackendId Backend { get; }
    public LanguageArtifactContract InputContract { get; }
    public LanguageRuntimeComponentTraits Traits { get; }
    public LanguageRuntimeComponentLifetime Lifetime { get; }
    internal bool IsOwnedBySession => Lifetime == LanguageRuntimeComponentLifetime.PerSession;

    public static LanguageExecutorRegistration Create<TInput, TResult>(
        LanguageContributionId contributionId,
        BackendId backend,
        LanguageArtifactKind<TInput> input,
        LanguageRuntimeComponentTraits traits,
        Func<LanguageRuntimeComponentContext, ILanguageArtifactExecutor<TInput, TResult>> factory,
        LanguageRuntimeComponentLifetime lifetime = LanguageRuntimeComponentLifetime.PerSession)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(factory);
        return new LanguageExecutorRegistration(
            contributionId,
            backend,
            input.Contract,
            traits,
            context => factory(context),
            lifetime);
    }

    public static LanguageExecutorRegistration FromStatelessSingleton(ILanguageArtifactExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);
        return new LanguageExecutorRegistration(
            executor.ContributionId,
            executor.Backend,
            executor.InputContract,
            executor.Traits,
            _ => executor,
            LanguageRuntimeComponentLifetime.SingletonStateless);
    }

    internal ILanguageArtifactExecutor Create(LanguageRuntimeComponentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Lifetime == LanguageRuntimeComponentLifetime.SingletonStateless)
        {
            lock (_gate)
            {
                _singleton ??= CreateAndValidate(context);
                return _singleton;
            }
        }

        var component = CreateAndValidate(context);
        if (!_createdInstances.TryAdd(component, CreatedMarker))
        {
            throw new InvalidOperationException(
                $"Executor factory for contribution '{ContributionId.Value}' reused an instance across runtime sessions. " +
                "Return a fresh component or opt in to SingletonStateless with an explicitly stateless component.");
        }
        return component;
    }

    private ILanguageArtifactExecutor CreateAndValidate(LanguageRuntimeComponentContext context)
    {
        var component = _factory(context)
            ?? throw new InvalidOperationException($"Executor factory for '{ContributionId.Value}' returned null.");
        if (component.ContributionId != ContributionId ||
            component.Backend != Backend ||
            component.InputContract != InputContract ||
            component.Traits != Traits)
        {
            throw new InvalidOperationException(
                $"Executor factory for '{ContributionId.Value}' returned a component whose identity, backend, contract, or traits do not match its registration.");
        }
        if (Lifetime == LanguageRuntimeComponentLifetime.SingletonStateless)
        {
            if (component is not IStatelessLanguageRuntimeComponent)
            {
                throw new InvalidOperationException(
                    $"Executor '{ContributionId.Value}' is registered as SingletonStateless but does not implement {nameof(IStatelessLanguageRuntimeComponent)}.");
            }
            if (component is IDisposable or IAsyncDisposable)
            {
                throw new InvalidOperationException(
                    $"Executor '{ContributionId.Value}' is registered as SingletonStateless but owns disposable resources. Use PerSession lifetime instead.");
            }
        }
        return component;
    }
}

public sealed class LanguageRouteComponentCatalog
{
    internal LanguageRouteComponentCatalog(
        IReadOnlyDictionary<LanguageContributionId, LanguageTransformerRegistration> transformers,
        IReadOnlyList<LanguageExecutorRegistration> executors)
    {
        Transformers = transformers;
        Executors = executors;
    }

    public IReadOnlyDictionary<LanguageContributionId, LanguageTransformerRegistration> Transformers { get; }
    public IReadOnlyList<LanguageExecutorRegistration> Executors { get; }
}

public interface ILanguageRouteComponentSource
{
    LanguagePackageDescriptor Descriptor { get; }
    LanguagePackageId PackageId => Descriptor.Id;
    LanguageVersion PackageVersion => Descriptor.Version;
    string ManifestSha256 => LanguageFeatureManifestSerializer.ComputeSha256(Descriptor);
    LanguageRouteComponentCatalog Components { get; }
}

public sealed class LanguageRouteComponentRegistry
{
    private readonly Dictionary<LanguageContributionId, LanguageTransformerRegistration> _transformers = [];
    private readonly List<LanguageExecutorRegistration> _executors = [];

    public LanguageRouteComponentRegistry AddTransformer(LanguageTransformerRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        if (!_transformers.TryAdd(registration.ContributionId, registration))
            throw new InvalidOperationException($"Transformer '{registration.ContributionId.Value}' is already registered.");
        return this;
    }

    public LanguageRouteComponentRegistry AddExecutor(LanguageExecutorRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        if (_executors.Any(existing =>
                existing.ContributionId == registration.ContributionId &&
                existing.Backend == registration.Backend &&
                existing.InputContract == registration.InputContract))
        {
            throw new InvalidOperationException(
                $"Executor contribution '{registration.ContributionId.Value}' is already registered for backend '{registration.Backend.Value}' and input '{registration.InputContract}'.");
        }
        _executors.Add(registration);
        return this;
    }

    public LanguageRouteComponentRegistry AddCatalog(LanguageRouteComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        foreach (var transformer in catalog.Transformers.Values.OrderBy(static item => item.ContributionId.Value, StringComparer.Ordinal))
            AddTransformer(transformer);
        foreach (var executor in catalog.Executors.OrderBy(static item => item.ContributionId.Value, StringComparer.Ordinal))
            AddExecutor(executor);
        return this;
    }

    public LanguageRouteComponentCatalog CreateCatalog() => new(
        new ReadOnlyDictionary<LanguageContributionId, LanguageTransformerRegistration>(
            new Dictionary<LanguageContributionId, LanguageTransformerRegistration>(_transformers)),
        new ReadOnlyCollection<LanguageExecutorRegistration>(_executors.ToArray()));

    internal IReadOnlyDictionary<LanguageContributionId, LanguageTransformerRegistration> SnapshotTransformers() =>
        CreateCatalog().Transformers;

    internal IReadOnlyList<LanguageExecutorRegistration> SnapshotExecutors() =>
        CreateCatalog().Executors;
}

public sealed class LanguageRouteRuntimeProvider : ILanguageRuntimeProvider, ILanguageRuntimePolicyValidator
{
    private readonly IReadOnlyDictionary<LanguageContributionId, LanguageTransformerRegistration> _transformers;
    private readonly IReadOnlyList<LanguageExecutorRegistration> _executors;
    private readonly IReadOnlyCollection<BackendId> _supportedBackends;

    public LanguageRouteRuntimeProvider(
        LanguageRuntimeProviderId providerId,
        LanguageVersion providerVersion,
        ToolchainApiVersion toolchainApiVersion,
        LanguageContributionId runtimeContributionId,
        LanguageRouteComponentRegistry components)
    {
        ProviderId = providerId;
        ProviderVersion = providerVersion;
        ToolchainApiVersion = toolchainApiVersion;
        RuntimeContributionId = runtimeContributionId;
        ArgumentNullException.ThrowIfNull(components);
        _transformers = components.SnapshotTransformers();
        _executors = components.SnapshotExecutors();
        _supportedBackends = new ReadOnlyCollection<BackendId>(
            _executors.Select(static x => x.Backend).Distinct().OrderBy(static x => x.Value, StringComparer.Ordinal).ToArray());
    }

    public LanguageRuntimeProviderId ProviderId { get; }
    public LanguageVersion ProviderVersion { get; }
    public ToolchainApiVersion ToolchainApiVersion { get; }
    public LanguageContributionId RuntimeContributionId { get; }
    public IReadOnlyCollection<BackendId> SupportedBackends => _supportedBackends;

    public void ValidatePolicy(LanguagePlan plan, LanguageRuntimePolicy policy, LanguageRuntimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(options);
        var selected = ValidateRouteImplementations(plan);
        var traits = selected.Transformers.Values.Select(static item => item.Traits)
            .Concat(selected.Executors.Values.Select(static item => item.Traits))
            .ToArray();

        if (policy.RequireDeterminism && traits.Any(static trait => trait.Determinism != LanguageComponentDeterminism.Deterministic))
        {
            throw new InvalidOperationException(
                "The language requires deterministic execution, but at least one selected runtime component is not declared deterministic.");
        }
        if (!policy.AllowHostInterop &&
            (traits.Any(static trait => trait.HostInterop != LanguageComponentHostInterop.None) ||
             options.AllowedAssemblies.Count != 0))
        {
            throw new InvalidOperationException(
                "The language forbids host interop, but at least one selected runtime component is not explicitly declared host-interop-free or runtime options enable interop.");
        }
    }

    public ILanguageRuntimeSession CreateSession(LanguagePlan plan, LanguageRuntimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(options);
        var selected = ValidateRouteImplementations(plan);
        var componentContext = new LanguageRuntimeComponentContext(plan, options);
        var transformers = new Dictionary<LanguageContributionId, ILanguageArtifactTransformer>();
        var executors = new Dictionary<BackendId, ILanguageArtifactExecutor>();
        var owned = new List<object>();
        var ownedSet = new HashSet<object>(ReferenceEqualityComparer.Instance);
        try
        {
            foreach (var registration in selected.Transformers.Values.OrderBy(static item => item.ContributionId.Value, StringComparer.Ordinal))
            {
                var component = registration.Create(componentContext);
                transformers.Add(registration.ContributionId, component);
                if (registration.IsOwnedBySession && ownedSet.Add(component))
                    owned.Add(component);
            }
            foreach (var pair in selected.Executors.OrderBy(static item => item.Key.Value, StringComparer.Ordinal))
            {
                var component = pair.Value.Create(componentContext);
                executors.Add(pair.Key, component);
                if (pair.Value.IsOwnedBySession && ownedSet.Add(component))
                    owned.Add(component);
            }
            return new RouteSession(
                plan,
                options,
                new ReadOnlyDictionary<LanguageContributionId, ILanguageArtifactTransformer>(transformers),
                new ReadOnlyDictionary<BackendId, ILanguageArtifactExecutor>(executors),
                owned);
        }
        catch
        {
            DisposeOwnedSynchronously(owned);
            throw;
        }
    }

    private SelectedRouteComponents ValidateRouteImplementations(LanguagePlan plan)
    {
        var selectedTransformers = new Dictionary<LanguageContributionId, LanguageTransformerRegistration>();
        var selectedExecutors = new Dictionary<BackendId, LanguageExecutorRegistration>();
        foreach (var route in plan.Routes.Values)
        {
            EnsureTypedRuntimeContract(route.SourceContract, $"route '{route.Backend.Value}' source");
            EnsureTypedRuntimeContract(route.TargetContract, $"route '{route.Backend.Value}' target");
            foreach (var step in route.Steps)
            {
                EnsureTypedRuntimeContract(step.SourceContract, $"transformer '{step.ContributionId.Value}' source");
                EnsureTypedRuntimeContract(step.TargetContract, $"transformer '{step.ContributionId.Value}' target");
                if (!_transformers.TryGetValue(step.ContributionId, out var transformer))
                {
                    throw new InvalidOperationException(
                        $"Artifact route requires transformer contribution '{step.ContributionId.Value}', but no implementation is registered.");
                }
                if (transformer.SourceContract != step.SourceContract || transformer.TargetContract != step.TargetContract)
                {
                    throw new InvalidOperationException(
                        $"Transformer '{step.ContributionId.Value}' implements '{transformer.SourceContract} -> {transformer.TargetContract}', " +
                        $"but the language plan requires '{step.SourceContract} -> {step.TargetContract}'.");
                }
                selectedTransformers.TryAdd(step.ContributionId, transformer);
            }

            var backendCapability = LanguageCapabilities.Backend(route.Backend);
            var backendOwners = plan.Contributions
                .Where(contribution => contribution.Contribution.ProvidesCapabilities.Contains(backendCapability))
                .ToArray();
            if (backendOwners.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Language plan must select exactly one backend contribution for '{route.Backend.Value}', but selected {backendOwners.Length}.");
            }
            var backendContributionId = backendOwners[0].Contribution.Id;
            var candidates = _executors
                .Where(executor => executor.ContributionId == backendContributionId &&
                                   executor.Backend == route.Backend &&
                                   executor.InputContract == route.TargetContract)
                .ToArray();
            if (candidates.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Artifact route for backend '{route.Backend.Value}' selects contribution '{backendContributionId.Value}' and ends at '{route.TargetContract}', but no matching executor implementation is registered.");
            }
            if (candidates.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Artifact route for backend '{route.Backend.Value}' and '{route.TargetContract}' has multiple compatible executors.");
            }
            selectedExecutors.Add(route.Backend, candidates[0]);
        }
        return new SelectedRouteComponents(
            new ReadOnlyDictionary<LanguageContributionId, LanguageTransformerRegistration>(selectedTransformers),
            new ReadOnlyDictionary<BackendId, LanguageExecutorRegistration>(selectedExecutors));
    }

    private static void EnsureTypedRuntimeContract(LanguageArtifactContract contract, string owner)
    {
        if (!contract.IsTyped)
        {
            throw new InvalidOperationException(
                $"Generic route runtime requires a typed artifact contract for {owner}; legacy untyped contracts must be hydrated before runtime assembly.");
        }
    }

    private sealed record SelectedRouteComponents(
        IReadOnlyDictionary<LanguageContributionId, LanguageTransformerRegistration> Transformers,
        IReadOnlyDictionary<BackendId, LanguageExecutorRegistration> Executors);

    private sealed class RouteSession : ILanguageRuntimeSession
    {
        private readonly LanguagePlan _plan;
        private readonly LanguageRuntimeOptions _options;
        private readonly IReadOnlyDictionary<LanguageContributionId, ILanguageArtifactTransformer> _transformers;
        private readonly IReadOnlyDictionary<BackendId, ILanguageArtifactExecutor> _executors;
        private readonly IReadOnlyList<object> _ownedComponents;
        private readonly RuntimeLifetimeGate _lifetime = new();

        public RouteSession(
            LanguagePlan plan,
            LanguageRuntimeOptions options,
            IReadOnlyDictionary<LanguageContributionId, ILanguageArtifactTransformer> transformers,
            IReadOnlyDictionary<BackendId, ILanguageArtifactExecutor> executors,
            IReadOnlyList<object> ownedComponents)
        {
            _plan = plan;
            _options = options;
            _transformers = transformers;
            _executors = executors;
            _ownedComponents = ownedComponents;
        }

        public LanguageExecutionResult Run(LanguageExecutionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            using var operation = _lifetime.EnterOperation(this);
            var route = _plan.Routes[request.Backend];
            var context = new LanguageArtifactTransformationContext(_plan, request, _options);
            LanguageArtifact current = request.Input;
            if (!LanguageArtifactRoute.ContractsConnect(current.Contract, route.SourceContract))
            {
                throw new InvalidOperationException(
                    $"Execution input '{current.Contract}' does not match route entry contract '{route.SourceContract}'.");
            }
            foreach (var step in route.Steps)
            {
                if (!LanguageArtifactRoute.ContractsConnect(current.Contract, step.SourceContract))
                {
                    throw new InvalidOperationException(
                        $"Route execution reached '{current.Contract}', but transformer '{step.ContributionId.Value}' expects '{step.SourceContract}'.");
                }
                current = _transformers[step.ContributionId].Transform(current, context)
                    ?? throw new InvalidOperationException($"Transformer '{step.ContributionId.Value}' returned null.");
                if (!LanguageArtifactRoute.ContractsConnect(current.Contract, step.TargetContract))
                {
                    throw new InvalidOperationException(
                        $"Transformer '{step.ContributionId.Value}' returned '{current.Contract}', but the route requires '{step.TargetContract}'.");
                }
            }
            var value = _executors[request.Backend].Execute(current, context);
            return new LanguageExecutionResult(request.Backend, value);
        }

        public void Dispose()
        {
            if (!_lifetime.BeginDispose())
                return;
            try
            {
                DisposeOwnedSynchronously(_ownedComponents);
            }
            finally
            {
                _lifetime.CompleteDispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_lifetime.BeginDispose())
                return;
            try
            {
                List<Exception>? errors = null;
                for (var index = _ownedComponents.Count - 1; index >= 0; index--)
                {
                    try
                    {
                        switch (_ownedComponents[index])
                        {
                            case IAsyncDisposable asyncDisposable:
                                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                                break;
                            case IDisposable disposable:
                                disposable.Dispose();
                                break;
                        }
                    }
                    catch (Exception exception)
                    {
                        (errors ??= []).Add(exception);
                    }
                }
                if (errors is { Count: > 0 })
                    throw new AggregateException("One or more language runtime components failed to dispose.", errors);
            }
            finally
            {
                _lifetime.CompleteDispose();
            }
        }
    }

    private static void DisposeOwnedSynchronously(IReadOnlyList<object> components)
    {
        List<Exception>? errors = null;
        for (var index = components.Count - 1; index >= 0; index--)
        {
            try
            {
                switch (components[index])
                {
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                    case IAsyncDisposable asyncDisposable:
                        asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                        break;
                }
            }
            catch (Exception exception)
            {
                (errors ??= []).Add(exception);
            }
        }
        if (errors is { Count: > 0 })
            throw new AggregateException("One or more language runtime components failed to dispose.", errors);
    }
}


public interface ILanguageRuntimeSession : IDisposable, IAsyncDisposable
{
    LanguageExecutionResult Run(LanguageExecutionRequest request);
    ValueTask IAsyncDisposable.DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

public interface ILanguageRuntimeProvider
{
    LanguageRuntimeProviderId ProviderId { get; }
    LanguageVersion ProviderVersion { get; }
    ToolchainApiVersion ToolchainApiVersion { get; }
    LanguageContributionId RuntimeContributionId { get; }
    IReadOnlyCollection<BackendId> SupportedBackends { get; }
    ILanguageRuntimeSession CreateSession(LanguagePlan plan, LanguageRuntimeOptions options);
}

public sealed class LanguageRuntimeProviderRegistry
{
    private readonly Dictionary<(LanguageRuntimeProviderId Id, LanguageVersion Version), ILanguageRuntimeProvider> _providers = [];

    public LanguageRuntimeProviderRegistry AddProvider(ILanguageRuntimeProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        var key = (provider.ProviderId, provider.ProviderVersion);
        if (_providers.TryGetValue(key, out var existing))
        {
            if (ReferenceEquals(existing, provider))
                return this;
            throw new InvalidOperationException(
                $"Runtime provider '{provider.ProviderId.Value}' version '{provider.ProviderVersion.Value}' is already registered.");
        }
        _providers.Add(key, provider);
        return this;
    }

    public ILanguageRuntimeProvider GetRequiredProvider(LanguageRuntimeProviderReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        if (_providers.TryGetValue((reference.ProviderId, reference.Version), out var provider))
            return provider;

        var availableVersions = _providers.Keys
            .Where(key => key.Id == reference.ProviderId)
            .Select(static key => key.Version.Value)
            .OrderBy(static version => version, StringComparer.Ordinal)
            .ToArray();
        if (availableVersions.Length == 0)
            throw new InvalidOperationException($"Runtime provider '{reference.ProviderId.Value}' is not registered.");
        throw new InvalidOperationException(
            $"Runtime provider '{reference.ProviderId.Value}' version '{reference.Version.Value}' is required; " +
            $"registered version(s): {string.Join(", ", availableVersions)}.");
    }
}
