namespace UniversalToolchain.PlanFuzz.Adapter.Acme;

internal sealed class AcmeActivationTrace
{
    private readonly object _gate = new();
    private readonly HashSet<string> _owners = new(StringComparer.Ordinal);

    public void RecordContribution(string contributionId)
    {
        if (string.IsNullOrWhiteSpace(contributionId))
            Thrower.Argument(nameof(contributionId), "Activation owner ID must not be empty.");
        lock (_gate)
            _owners.Add(SurfaceContribution(contributionId));
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (_gate)
        {
            return new ReadOnlyCollection<string>(_owners
                .OrderBy(static owner => owner, StringComparer.Ordinal)
                .ToArray());
        }
    }

    public static string SurfaceFeature(string id) => $"feature:{id}";
    public static string SurfaceContribution(string id) => $"contribution:{id}";
}

internal sealed class AcmeIndependentExtensionRuntimeHook
{
    private readonly AcmeActivationTrace _trace;

    public AcmeIndependentExtensionRuntimeHook(AcmeActivationTrace trace) =>
        _trace = trace.ArgNotNull();

    public string Transform(string value)
    {
        Activate();
        return value;
    }

    public decimal Interfere(decimal value)
    {
        Activate();
        return value + 1m;
    }

    public void Activate() =>
        _trace.RecordContribution(AcmePlanFuzzConstants.IndependentContributionId);
}

internal enum AcmeRuntimeFaultMode
{
    None,
    ActivateExcludedOwner,
    ExtensionInterference,
    UnknownOwnerEvidence
}

internal sealed class AcmeRecordingRuntimeProvider : ILanguageRuntimeProvider, ILanguageRuntimePolicyValidator
{
    private readonly LanguageRouteRuntimeProvider _inner;
    private readonly AcmeActivationTrace _trace;
    private readonly AcmeIndependentExtensionRuntimeHook _independentExtension;
    private readonly AcmeRuntimeFaultMode _faultMode;

    public AcmeRecordingRuntimeProvider(
        LanguageRouteRuntimeProvider inner,
        AcmeActivationTrace trace,
        AcmeIndependentExtensionRuntimeHook independentExtension,
        AcmeRuntimeFaultMode faultMode)
    {
        _inner = inner.ArgNotNull();
        _trace = trace.ArgNotNull();
        _independentExtension = independentExtension.ArgNotNull();
        _faultMode = faultMode;
    }

    public LanguageRuntimeProviderId ProviderId => _inner.ProviderId;
    public LanguageVersion ProviderVersion => _inner.ProviderVersion;
    public ToolchainApiVersion ToolchainApiVersion => _inner.ToolchainApiVersion;
    public LanguageContributionId RuntimeContributionId => _inner.RuntimeContributionId;
    public IReadOnlyCollection<BackendId> SupportedBackends => _inner.SupportedBackends;

    public void ValidatePolicy(LanguagePlan plan, LanguageRuntimePolicy policy, LanguageRuntimeOptions options) =>
        _inner.ValidatePolicy(plan, policy, options);

    public ILanguageRuntimeSession CreateSession(LanguagePlan plan, LanguageRuntimeOptions options) =>
        new RecordingSession(
            _inner.CreateSession(plan, options),
            _trace,
            _independentExtension,
            RuntimeContributionId,
            _faultMode);

    private sealed class RecordingSession : ILanguageRuntimeSession
    {
        private readonly ILanguageRuntimeSession _inner;
        private readonly AcmeActivationTrace _trace;
        private readonly AcmeIndependentExtensionRuntimeHook _independentExtension;
        private readonly LanguageContributionId _runtimeContributionId;
        private readonly AcmeRuntimeFaultMode _faultMode;

        public RecordingSession(
            ILanguageRuntimeSession inner,
            AcmeActivationTrace trace,
            AcmeIndependentExtensionRuntimeHook independentExtension,
            LanguageContributionId runtimeContributionId,
            AcmeRuntimeFaultMode faultMode)
        {
            _inner = inner.ArgNotNull();
            _trace = trace.ArgNotNull();
            _independentExtension = independentExtension.ArgNotNull();
            _runtimeContributionId = runtimeContributionId;
            _faultMode = faultMode;
        }

        public LanguageExecutionResult Run(LanguageExecutionRequest request)
        {
            _trace.RecordContribution(_runtimeContributionId.Value);
            if (_faultMode == AcmeRuntimeFaultMode.ActivateExcludedOwner)
                _independentExtension.Activate();
            else if (_faultMode == AcmeRuntimeFaultMode.UnknownOwnerEvidence)
                _trace.RecordContribution(AcmePlanFuzzConstants.UnknownOwnerId);

            var result = _inner.Run(request);
            if (_faultMode != AcmeRuntimeFaultMode.ExtensionInterference)
                return result;

            if (result.Value is not decimal value)
                throw new InvalidOperationException("The Acme extension-interference fault requires a decimal result.");
            return new LanguageExecutionResult(result.Backend, _independentExtension.Interfere(value));
        }

        public void Dispose() => _inner.Dispose();
        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }
}
