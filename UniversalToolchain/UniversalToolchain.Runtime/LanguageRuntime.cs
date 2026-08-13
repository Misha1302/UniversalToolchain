using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Runtime;

/// <summary>
/// Materializes and executes one immutable <see cref="LanguagePlan"/>.
/// Artifact construction is exposed only by <see cref="LanguageBuildRuntime"/>.
/// </summary>
public class LanguageRuntime : IDisposable, IAsyncDisposable
{
    private readonly LanguagePlan _plan;
    private readonly ILanguageRuntimeSession _session;
    private readonly RuntimeLifetimeGate _lifetime = new();

    private protected LanguageRuntime(LanguagePlan plan, ILanguageRuntimeSession session)
    {
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public LanguagePlan Plan => _plan;

    private protected ILanguageRuntimeSession SessionOwner => _session;

    public static LanguageRuntime Create(
        LanguagePlan plan,
        LanguageRuntimeProviderRegistry providers,
        LanguageRuntimeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(providers);
        if (plan.RuntimeProvider == null)
            throw new InvalidOperationException("The language plan is planning-only and has no runtime provider.");

        options ??= new LanguageRuntimeOptions();
        var provider = providers.GetRequiredProvider(plan.RuntimeProvider);
        return new LanguageRuntime(plan, CreateSessionCore(plan, provider, options));
    }

    /// <summary>
    /// Creates a runtime with the typed artifact-build capability because exact route component
    /// registrations are available from the supplied package sources.
    /// The returned type can still be consumed as <see cref="LanguageRuntime"/> when only execution is required.
    /// </summary>
    public static LanguageBuildRuntime Create(
        LanguagePlan plan,
        IEnumerable<ILanguageRouteComponentSource> componentSources,
        LanguageRuntimeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(componentSources);
        options ??= new LanguageRuntimeOptions();

        var registry = LanguageRouteRuntimeAssembler.CreateRegistry(plan, componentSources);
        var provider = LanguageRouteRuntimeAssembler.CreateProvider(plan, registry);
        var session = CreateSessionCore(plan, provider, options);
        try
        {
            var buildSession = new LanguageArtifactBuildPipeline(plan, options, registry);
            return new LanguageBuildRuntime(plan, session, buildSession);
        }
        catch (Exception primaryException)
        {
            var cleanupExceptions = RuntimeConstructionFailure.DisposeSynchronouslyCollect(session);
            RuntimeConstructionFailure.Rethrow(
                primaryException,
                cleanupExceptions,
                "Language build runtime construction failed and runtime-session cleanup also failed.");
            throw;
        }
    }

    public static LanguageRuntime Create(
        LanguagePlan plan,
        ILanguageRuntimeProvider provider,
        LanguageRuntimeOptions? options = null)
    {
        options ??= new LanguageRuntimeOptions();
        return new LanguageRuntime(plan, CreateSessionCore(plan, provider, options));
    }

    private static ILanguageRuntimeSession CreateSessionCore(
        LanguagePlan plan,
        ILanguageRuntimeProvider provider,
        LanguageRuntimeOptions options)
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

        return provider.CreateSession(plan, options);
    }

    public LanguageExecutionResult Run(LanguageExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var operation = EnterRuntimeOperation();
        ValidateOperationInput(request.Input, request.Backend, request.Arguments.Count, "Execution");
        return _session.Run(request);
    }

    private protected IDisposable EnterRuntimeOperation() => _lifetime.EnterOperation(this);

    private protected void ValidateOperationInput(
        LanguageArtifact input,
        BackendId backend,
        int parameterCount,
        string operation)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        if (!_plan.Definition.Backends.Contains(backend))
            throw new InvalidOperationException($"Backend '{backend.Value}' is not enabled by language plan '{_plan.PlanHash}'.");
        if (!LanguageArtifactRoute.ContractsConnect(input.Contract, _plan.Definition.EntryArtifact))
        {
            throw new InvalidOperationException(
                $"{operation} input '{input.Contract}' does not match language entry artifact '{_plan.Definition.EntryArtifact}'.");
        }
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
            DisposeCore();
        }
        finally
        {
            _lifetime.CompleteDispose();
        }
    }

    private protected virtual void DisposeCore() => _session.Dispose();

    public async ValueTask DisposeAsync()
    {
        if (!_lifetime.BeginDispose())
            return;
        try
        {
            await DisposeAsyncCore().ConfigureAwait(false);
        }
        finally
        {
            _lifetime.CompleteDispose();
        }
    }

    private protected virtual ValueTask DisposeAsyncCore() => _session.DisposeAsync();
}
