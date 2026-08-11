using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Runtime;

/// <summary>
/// A language runtime that can both execute a planned language and materialize exact planned artifacts.
/// Instances are created only from exact <see cref="ILanguageRouteComponentSource"/> registrations.
/// </summary>
public sealed class LanguageBuildRuntime : IDisposable, IAsyncDisposable
{
    private readonly LanguageRuntime _runtime;
    private readonly ILanguageArtifactBuildSession _buildSession;
    private readonly RuntimeLifetimeGate _lifetime = new();

    internal LanguageBuildRuntime(LanguageRuntime runtime, ILanguageArtifactBuildSession buildSession)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _buildSession = buildSession ?? throw new ArgumentNullException(nameof(buildSession));
    }

    public LanguagePlan Plan => _runtime.Plan;

    public LanguageExecutionResult Run(LanguageExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var operation = _lifetime.EnterOperation(this);
        return _runtime.Run(request);
    }

    public LanguageArtifactBuildResult Build(LanguageArtifactBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var operation = _lifetime.EnterOperation(this);
        ValidateOperationInput(request.Input, request.Backend, request.Bindings.Count);
        return _buildSession.Build(request);
    }

    public LanguageExecutionResult ExecuteBuilt(LanguageArtifactBuildResult artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        using var operation = _lifetime.EnterOperation(this);
        return _buildSession.ExecuteBuilt(artifact);
    }

    public T GetBuiltArtifactValue<T>(LanguageArtifactBuildResult artifact, LanguageArtifactKind<T> expectedKind)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(expectedKind);
        using var operation = _lifetime.EnterOperation(this);
        return _buildSession.GetBuiltArtifactValue(artifact, expectedKind);
    }

    private void ValidateOperationInput(LanguageArtifact input, BackendId backend, int parameterCount)
    {
        var plan = Plan;
        if (!plan.Definition.Backends.Contains(backend))
            throw new InvalidOperationException($"Backend '{backend.Value}' is not enabled by language plan '{plan.PlanHash}'.");
        if (!LanguageArtifactRoute.ContractsConnect(input.Contract, plan.Definition.EntryArtifact))
            throw new InvalidOperationException(
                $"Build input '{input.Contract}' does not match language entry artifact '{plan.Definition.EntryArtifact}'.");
        if (plan.Definition.RuntimePolicy.MaximumSourceLength is int maxSource &&
            input is LanguageArtifact<string> sourceArtifact && sourceArtifact.Value.Length > maxSource)
        {
            throw new InvalidOperationException($"Source length {sourceArtifact.Value.Length} exceeds language policy limit {maxSource}.");
        }
        if (plan.Definition.RuntimePolicy.MaximumExternalParameters is int maxParameters && parameterCount > maxParameters)
            throw new InvalidOperationException($"External parameter count {parameterCount} exceeds language policy limit {maxParameters}.");
    }

    public void Dispose()
    {
        if (!_lifetime.BeginDispose())
            return;
        try
        {
            var errors = RuntimeConstructionFailure.DisposeSynchronouslyCollect(_runtime, _buildSession);
            if (errors.Count != 0)
                throw new AggregateException("One or more language build runtime owners failed to dispose.", errors);
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
            var errors = await RuntimeConstructionFailure.DisposeAsynchronouslyCollect(_runtime, _buildSession)
                .ConfigureAwait(false);
            if (errors.Count != 0)
                throw new AggregateException("One or more language build runtime owners failed to dispose.", errors);
        }
        finally
        {
            _lifetime.CompleteDispose();
        }
    }
}
