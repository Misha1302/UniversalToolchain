using System.Collections.ObjectModel;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Runtime;

internal sealed class LanguageArtifactBuildPipeline : ILanguageArtifactBuildSession, IDisposable, IAsyncDisposable
{
    private readonly LanguagePlan _plan;
    private readonly LanguageRuntimeOptions _options;
    private readonly IReadOnlyDictionary<LanguageContributionId, LanguageTransformerRegistration> _transformerRegistrations;
    private readonly IReadOnlyDictionary<BackendId, LanguageExecutorRegistration> _executorRegistrations;
    private readonly Dictionary<LanguageContributionId, ILanguageArtifactTransformer> _transformers = [];
    private readonly Dictionary<BackendId, ILanguageArtifactExecutor> _executors = [];
    private readonly LanguageRuntimeComponentContext _componentContext;
    private readonly List<object> _ownedComponents = [];
    private readonly HashSet<object> _ownedSet = new(ReferenceEqualityComparer.Instance);
    private readonly object _componentGate = new();
    private readonly RuntimeLifetimeGate _lifetime = new();
    private readonly object _ownerToken = new();

    public LanguageArtifactBuildPipeline(
        LanguagePlan plan,
        LanguageRuntimeOptions options,
        LanguageRouteComponentRegistry components)
    {
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(components);

        _componentContext = new LanguageRuntimeComponentContext(plan, options);
        _transformerRegistrations = new ReadOnlyDictionary<LanguageContributionId, LanguageTransformerRegistration>(
            new Dictionary<LanguageContributionId, LanguageTransformerRegistration>(components.SnapshotTransformers()));
        _executorRegistrations = new ReadOnlyDictionary<BackendId, LanguageExecutorRegistration>(
            BindExecutorRegistrations(plan, components.SnapshotExecutors()));
    }

    public LanguageArtifactBuildResult Build(LanguageArtifactBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var operation = _lifetime.EnterOperation(this);
        if (!_plan.Routes.TryGetValue(request.Backend, out var route))
            throw new InvalidOperationException($"Language plan contains no artifact route for backend '{request.Backend.Value}'.");
        if (request.Input.Contract != route.SourceContract)
        {
            throw new InvalidOperationException(
                $"Build input '{request.Input.Contract}' does not exactly match route entry contract '{route.SourceContract}'.");
        }

        var executionRequest = request.ToExecutionRequest();
        var buildContext = new LanguageArtifactBuildContext(_plan, request, _options, executionRequest);
        LanguageArtifact current = request.Input;
        var steps = new List<LanguageArtifactBuildStep>(route.Steps.Count);
        for (var stepIndex = 0; stepIndex < route.Steps.Count; stepIndex++)
        {
            var step = route.Steps[stepIndex];
            if (current.Contract != step.SourceContract)
            {
                throw new InvalidOperationException(
                    $"Build reached '{current.Contract}', but transformer '{step.ContributionId.Value}' expects '{step.SourceContract}'.");
            }
            var transformer = GetOrCreateTransformer(step.ContributionId);
            current = transformer is ILanguageArtifactBuildTransformer buildTransformer
                ? buildTransformer.TransformForBuild(current, buildContext)
                : transformer.Transform(current, buildContext.TransformationContext);
            if (current == null)
                throw new InvalidOperationException($"Transformer '{step.ContributionId.Value}' returned null during artifact build.");
            if (current.Contract != step.TargetContract)
            {
                throw new InvalidOperationException(
                    $"Transformer '{step.ContributionId.Value}' returned '{current.Contract}', but the build route requires '{step.TargetContract}'.");
            }
            LanguageArtifactRouteObservationDispatcher.Notify(
                _plan, executionRequest, _options, request.Backend, route.Steps, stepIndex, step, current);
            steps.Add(new LanguageArtifactBuildStep(step.ContributionId, step.SourceContract, step.TargetContract));
        }

        if (current.Contract != route.TargetContract)
        {
            throw new InvalidOperationException(
                $"Artifact build ended at '{current.Contract}', but backend '{request.Backend.Value}' requires '{route.TargetContract}'.");
        }

        return new LanguageArtifactBuildResult(
            _plan.Definition.Id,
            _plan.Definition.Version,
            _plan.PlanHash,
            request.Backend,
            current,
            steps,
            [],
            executionRequest,
            _ownerToken);
    }

    public LanguageExecutionResult ExecuteBuilt(LanguageArtifactBuildResult artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        using var operation = _lifetime.EnterOperation(this);
        ValidateArtifactBinding(artifact);
        var executor = GetOrCreateExecutor(artifact.Backend);
        var context = new LanguageArtifactTransformationContext(_plan, artifact.ExecutionRequest, _options);
        return new LanguageExecutionResult(artifact.Backend, executor.Execute(artifact.Artifact, context));
    }

    public T GetBuiltArtifactValue<T>(LanguageArtifactBuildResult artifact, LanguageArtifactKind<T> expectedKind)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(expectedKind);
        using var operation = _lifetime.EnterOperation(this);
        ValidateArtifactBinding(artifact);
        if (artifact.ArtifactContract != expectedKind.Contract)
        {
            throw new InvalidOperationException(
                $"Built artifact contract '{artifact.ArtifactContract}' does not match requested contract '{expectedKind.Contract}'.");
        }
        return artifact.Artifact.GetRequiredValue<T>();
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
                throw new AggregateException("One or more artifact build components failed to dispose.", errors);
        }
        finally
        {
            _lifetime.CompleteDispose();
        }
    }

    private ILanguageArtifactTransformer GetOrCreateTransformer(LanguageContributionId contributionId)
    {
        lock (_componentGate)
        {
            if (_transformers.TryGetValue(contributionId, out var transformer))
                return transformer;

            var registration = _transformerRegistrations[contributionId];
            transformer = registration.Create(_componentContext);
            _transformers.Add(contributionId, transformer);
            TrackOwnedLocked(registration.IsOwnedBySession, transformer);
            return transformer;
        }
    }

    private ILanguageArtifactExecutor GetOrCreateExecutor(BackendId backend)
    {
        lock (_componentGate)
        {
            if (_executors.TryGetValue(backend, out var executor))
                return executor;

            var registration = _executorRegistrations[backend];
            executor = registration.Create(_componentContext);
            _executors.Add(backend, executor);
            TrackOwnedLocked(registration.IsOwnedBySession, executor);
            return executor;
        }
    }

    private void TrackOwnedLocked(bool owned, object component)
    {
        if (owned && _ownedSet.Add(component))
            _ownedComponents.Add(component);
    }

    private void ValidateArtifactBinding(LanguageArtifactBuildResult artifact)
    {
        if (!ReferenceEquals(artifact.OwnerToken, _ownerToken))
            throw new InvalidOperationException("Built artifact belongs to a different language runtime/build session.");
        if (!StringComparer.Ordinal.Equals(artifact.PlanHash, _plan.PlanHash) ||
            artifact.LanguageId != _plan.Definition.Id ||
            artifact.LanguageVersion != _plan.Definition.Version)
        {
            throw new InvalidOperationException("Built artifact plan identity does not match the originating language plan.");
        }
        if (!_plan.Routes.TryGetValue(artifact.Backend, out var route))
            throw new InvalidOperationException($"Built artifact backend '{artifact.Backend.Value}' is not enabled by the language plan.");
        if (artifact.ArtifactContract != route.TargetContract)
        {
            throw new InvalidOperationException(
                $"Built artifact contract '{artifact.ArtifactContract}' does not match backend '{artifact.Backend.Value}' route target '{route.TargetContract}'.");
        }
    }

    private static Dictionary<BackendId, LanguageExecutorRegistration> BindExecutorRegistrations(
        LanguagePlan plan,
        IReadOnlyList<LanguageExecutorRegistration> registrations)
    {
        var result = new Dictionary<BackendId, LanguageExecutorRegistration>();
        foreach (var route in plan.Routes.Values)
        {
            var backendCapability = LanguageCapabilities.Backend(route.Backend);
            var backendContribution = plan.Contributions.Single(
                contribution => contribution.Contribution.ProvidesCapabilities.Contains(backendCapability));
            var matches = registrations.Where(registration =>
                    registration.ContributionId == backendContribution.Contribution.Id &&
                    registration.Backend == route.Backend &&
                    registration.InputContract == route.TargetContract)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Artifact build pipeline requires exactly one executor registration for backend '{route.Backend.Value}' and route target '{route.TargetContract}', but {matches.Length} were found.");
            }
            result.Add(route.Backend, matches[0]);
        }
        return result;
    }

    private static void DisposeOwnedSynchronously(IReadOnlyList<object> components)
    {
        var errors = DisposeOwnedSynchronouslyCollect(components);
        if (errors.Count != 0)
            throw new AggregateException("One or more artifact build components failed to dispose.", errors);
    }

    private static IReadOnlyList<Exception> DisposeOwnedSynchronouslyCollect(IReadOnlyList<object> components)
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
        return errors ?? [];
    }
}
