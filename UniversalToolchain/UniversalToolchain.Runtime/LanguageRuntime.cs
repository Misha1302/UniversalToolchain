using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Runtime;

public sealed class LanguageRuntime : IDisposable, IAsyncDisposable
{
    private readonly LanguagePlan _plan;
    private readonly ILanguageRuntimeSession _session;
    private readonly ILanguageArtifactBuildSession? _buildSession;
    private readonly RuntimeLifetimeGate _lifetime = new();

    private LanguageRuntime(
        LanguagePlan plan,
        ILanguageRuntimeSession session,
        ILanguageArtifactBuildSession? buildSession)
    {
        _plan = plan;
        _session = session;
        _buildSession = buildSession;
    }

    public LanguagePlan Plan => _plan;

    public static LanguageRuntime Create(
        LanguagePlan plan,
        LanguageRuntimeProviderRegistry providers,
        LanguageRuntimeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(providers);
        if (plan.RuntimeProvider == null)
            throw new InvalidOperationException("The language plan is planning-only and has no runtime provider.");
        return Create(plan, providers.GetRequiredProvider(plan.RuntimeProvider), options);
    }

    public static LanguageRuntime Create(
        LanguagePlan plan,
        IEnumerable<ILanguageRouteComponentSource> componentSources,
        LanguageRuntimeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(componentSources);
        options ??= new LanguageRuntimeOptions();
        var registry = LanguageRouteRuntimeAssembler.CreateRegistry(plan, componentSources);
        var provider = LanguageRouteRuntimeAssembler.CreateProvider(plan, registry);
        return CreateCore(
            plan,
            provider,
            options,
            () => new LanguageArtifactBuildPipeline(plan, options, registry));
    }

    public static LanguageRuntime Create(
        LanguagePlan plan,
        ILanguageRuntimeProvider provider,
        LanguageRuntimeOptions? options = null)
    {
        options ??= new LanguageRuntimeOptions();
        return CreateCore(plan, provider, options, null);
    }

    private static LanguageRuntime CreateCore(
        LanguagePlan plan,
        ILanguageRuntimeProvider provider,
        LanguageRuntimeOptions options,
        Func<ILanguageArtifactBuildSession?>? buildSessionFactory)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(options);
        LanguagePlanVerifier.Verify(plan);

        if (plan.RuntimeProvider == null || plan.RuntimeProviderContribution == null)
            throw new InvalidOperationException("The language plan is planning-only and cannot create a runtime session.");
        if (provider.ProviderId != plan.RuntimeProvider.ProviderId ||
            provider.ProviderVersion != plan.RuntimeProvider.Version)
        {
            throw new InvalidOperationException(
                $"Language plan requires runtime provider '{plan.RuntimeProvider.ProviderId.Value}' version '{plan.RuntimeProvider.Version.Value}', " +
                $"but '{provider.ProviderId.Value}' version '{provider.ProviderVersion.Value}' was supplied.");
        }
        if (provider.ToolchainApiVersion != plan.Definition.ToolchainApiVersion)
            throw new InvalidOperationException("Runtime provider Toolchain API version does not match the language plan.");
        if (provider.RuntimeContributionId != plan.RuntimeProviderContribution.Contribution.Id)
        {
            throw new InvalidOperationException(
                $"Runtime provider '{provider.ProviderId.Value}' implements contribution '{provider.RuntimeContributionId.Value}', " +
                $"but the plan selected '{plan.RuntimeProviderContribution.Contribution.Id.Value}'.");
        }
        var missingBackends = plan.Definition.Backends
            .Where(backend => !provider.SupportedBackends.Contains(backend))
            .ToArray();
        if (missingBackends.Length != 0)
        {
            throw new InvalidOperationException(
                $"Runtime provider does not support backend(s): {string.Join(", ", missingBackends.Select(static x => x.Value))}.");
        }
        foreach (var backend in plan.Definition.Backends)
        {
            if (!plan.Routes.ContainsKey(backend))
                throw new InvalidOperationException($"Language plan contains no artifact route for backend '{backend.Value}'.");
        }

        var policyValidator = provider as ILanguageRuntimePolicyValidator;
        if (policyValidator == null &&
            (plan.Definition.RuntimePolicy.RequireDeterminism || !plan.Definition.RuntimePolicy.AllowHostInterop))
        {
            throw new InvalidOperationException(
                $"Runtime provider '{provider.ProviderId.Value}' does not implement runtime-policy validation.");
        }
        policyValidator?.ValidatePolicy(plan, plan.Definition.RuntimePolicy, options);

        var session = provider.CreateSession(plan, options);
        try
        {
            var buildSession = buildSessionFactory?.Invoke();
            return new LanguageRuntime(plan, session, buildSession);
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    public LanguageExecutionResult Run(LanguageExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var operation = _lifetime.EnterOperation(this);
        ValidateOperationInput(request.Input, request.Backend, request.Arguments.Count);
        return _session.Run(request);
    }

    public LanguageArtifactBuildResult Build(LanguageArtifactBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var operation = _lifetime.EnterOperation(this);
        var buildSession = _buildSession ?? throw new NotSupportedException(
            "Build-only artifact construction requires LanguageRuntime.Create(plan, componentSources, options) so exact route registrations remain available without discovery.");
        ValidateOperationInput(request.Input, request.Backend, request.Bindings.Count);
        return buildSession.Build(request);
    }

    public LanguageExecutionResult ExecuteBuilt(LanguageArtifactBuildResult artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        using var operation = _lifetime.EnterOperation(this);
        var buildSession = _buildSession ?? throw new NotSupportedException(
            "This language runtime was not created with an artifact build session.");
        return buildSession.ExecuteBuilt(artifact);
    }

    public T GetBuiltArtifactValue<T>(LanguageArtifactBuildResult artifact, LanguageArtifactKind<T> expectedKind)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(expectedKind);
        using var operation = _lifetime.EnterOperation(this);
        var buildSession = _buildSession ?? throw new NotSupportedException(
            "This language runtime was not created with an artifact build session.");
        return buildSession.GetBuiltArtifactValue(artifact, expectedKind);
    }

    private void ValidateOperationInput(LanguageArtifact input, BackendId backend, int parameterCount)
    {
        if (!_plan.Definition.Backends.Contains(backend))
            throw new InvalidOperationException($"Backend '{backend.Value}' is not enabled by language plan '{_plan.PlanHash}'.");
        if (!LanguageArtifactRoute.ContractsConnect(input.Contract, _plan.Definition.EntryArtifact))
            throw new InvalidOperationException(
                $"Execution input '{input.Contract}' does not match language entry artifact '{_plan.Definition.EntryArtifact}'.");
        if (_plan.Definition.RuntimePolicy.MaximumSourceLength is int maxSource &&
            input is LanguageArtifact<string> sourceArtifact && sourceArtifact.Value.Length > maxSource)
        {
            throw new InvalidOperationException($"Source length {sourceArtifact.Value.Length} exceeds language policy limit {maxSource}.");
        }
        if (_plan.Definition.RuntimePolicy.MaximumExternalParameters is int maxParameters && parameterCount > maxParameters)
            throw new InvalidOperationException($"External parameter count {parameterCount} exceeds language policy limit {maxParameters}.");
    }

    public void Dispose()
    {
        if (!_lifetime.BeginDispose())
            return;
        try
        {
            List<Exception>? errors = null;
            try
            {
                (_buildSession as IDisposable)?.Dispose();
            }
            catch (Exception exception)
            {
                (errors ??= []).Add(exception);
            }
            try
            {
                _session.Dispose();
            }
            catch (Exception exception)
            {
                (errors ??= []).Add(exception);
            }
            if (errors is { Count: > 0 })
                throw new AggregateException("One or more language runtime owners failed to dispose.", errors);
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
            try
            {
                if (_buildSession is IAsyncDisposable asyncBuildSession)
                    await asyncBuildSession.DisposeAsync().ConfigureAwait(false);
                else
                    (_buildSession as IDisposable)?.Dispose();
            }
            catch (Exception exception)
            {
                (errors ??= []).Add(exception);
            }
            try
            {
                await _session.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                (errors ??= []).Add(exception);
            }
            if (errors is { Count: > 0 })
                throw new AggregateException("One or more language runtime owners failed to dispose.", errors);
        }
        finally
        {
            _lifetime.CompleteDispose();
        }
    }
}
