using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Runtime;

public sealed class LanguageRuntime : IDisposable, IAsyncDisposable
{
    private readonly LanguagePlan _plan;
    private readonly ILanguageRuntimeSession _session;
    private readonly RuntimeLifetimeGate _lifetime = new();

    private LanguageRuntime(LanguagePlan plan, ILanguageRuntimeSession session)
    {
        _plan = plan;
        _session = session;
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
        LanguageRuntimeOptions? options = null) =>
        Create(plan, LanguageRouteRuntimeAssembler.CreateProvider(plan, componentSources), options);

    public static LanguageRuntime Create(
        LanguagePlan plan,
        ILanguageRuntimeProvider provider,
        LanguageRuntimeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(provider);
        LanguagePlanVerifier.Verify(plan);
        options ??= new LanguageRuntimeOptions();

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

        return new LanguageRuntime(plan, provider.CreateSession(plan, options));
    }


    public LanguageExecutionResult Run(LanguageExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var operation = _lifetime.EnterOperation(this);
        if (!_plan.Definition.Backends.Contains(request.Backend))
            throw new InvalidOperationException($"Backend '{request.Backend.Value}' is not enabled by language plan '{_plan.PlanHash}'.");
        if (!LanguageArtifactRoute.ContractsConnect(request.Input.Contract, _plan.Definition.EntryArtifact))
            throw new InvalidOperationException(
                $"Execution input '{request.Input.Contract}' does not match language entry artifact '{_plan.Definition.EntryArtifact}'.");
        if (_plan.Definition.RuntimePolicy.MaximumSourceLength is int maxSource &&
            request.Input is LanguageArtifact<string> sourceArtifact && sourceArtifact.Value.Length > maxSource)
        {
            throw new InvalidOperationException($"Source length {sourceArtifact.Value.Length} exceeds language policy limit {maxSource}.");
        }
        if (_plan.Definition.RuntimePolicy.MaximumExternalParameters is int maxParameters && request.Arguments.Count > maxParameters)
            throw new InvalidOperationException($"External parameter count {request.Arguments.Count} exceeds language policy limit {maxParameters}.");
        return _session.Run(request);
    }

    public void Dispose()
    {
        if (!_lifetime.BeginDispose())
            return;
        try
        {
            _session.Dispose();
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
            await _session.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _lifetime.CompleteDispose();
        }
    }
}
