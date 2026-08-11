using ExceptionsManager;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.ModuleContracts;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.Wist;

/// <summary>
/// Public Wist facade over one canonical LanguagePlan and exact LanguageBuildRuntime.
/// Planning occurs only during Create; hot operations reuse the same plan/runtime.
/// One instance intentionally rejects overlapping public operations; use separate engines for concurrency.
/// </summary>
public sealed class WistEngine : IDisposable
{
    private LanguageBuildRuntime? _runtime;
    private readonly LanguagePlan _plan;
    private readonly BackendId _backend;
    private readonly WistEngineOptions _options;
    private readonly WistResourceLimits _resourceLimits;
    private readonly RuntimeLifetimeGate _lifetime = new();
    private readonly WistOperationConcurrencyGate _concurrency = new();

    private WistEngine(
        LanguageBuildRuntime runtime,
        LanguagePlan plan,
        BackendId backend,
        WistEngineOptions options,
        WistResourceLimits resourceLimits)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _backend = backend;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _resourceLimits = resourceLimits ?? throw new ArgumentNullException(nameof(resourceLimits));
    }

    public void Dispose()
    {
        if (!_lifetime.BeginDispose())
            return;
        try
        {
            var runtime = Interlocked.Exchange(ref _runtime, null);
            runtime?.Dispose();
        }
        finally
        {
            _lifetime.CompleteDispose();
        }
    }

    public static WistEngine CreateRestrictedArithmetic() =>
        Create(WistEngineOptions.FromPresetId(WistLanguageDefinitions.PricingRestrictedId));

    public static WistEngine CreateFullNative() =>
        Create(WistEngineOptions.FromPresetId(WistLanguageDefinitions.FullDefaultNativeId));

    /// <summary>
    /// Creates a Wist engine from public facade options. Options are snapshotted at creation time.
    /// Planning happens exactly once here; Evaluate/Validate/Compile reuse the same LanguagePlan.
    /// </summary>
    public static WistEngine Create(WistEngineOptions options)
    {
        options = options.ArgNotNull();
        var resourceLimits = options.ResourceLimits.ArgNotNull().SnapshotValidated();
        var optimization = options.Optimization.ArgNotNull().SnapshotValidated();
        var allowedAssemblies = options.AllowedAssemblies.ArgNotNull().ToArray();

        if (allowedAssemblies.Any(static assembly => assembly is null))
            Thrower.Argument(nameof(options.AllowedAssemblies), "The allowed assembly collection must not contain null values.");

        var backend = new BackendId(RequireBackendId(options.BackendId));
        var optionsSnapshot = new WistEngineOptions
        {
            DialectSource = options.DialectSource.ArgNotNull(),
            BackendId = backend.Value,
            AllowedAssemblies = allowedAssemblies,
            ResourceLimits = resourceLimits,
            Optimization = optimization,
            SourceRetention = RequireSourceRetention(options.SourceRetention),
            DiagnosticExposure = RequireDiagnosticExposure(options.DiagnosticExposure),
            VerificationPolicy = RequireVerificationPolicy(options.VerificationPolicy)
        };

        var package = new WistLanguageFeaturePackage();
        var definition = ResolveLanguageDefinition(optionsSnapshot);
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();
        var runtimeAssemblies = plan.Definition.RuntimePolicy.AllowHostInterop
            ? allowedAssemblies
            : Array.Empty<System.Reflection.Assembly>();
        var runtime = LanguageRuntime.Create(
            plan,
            new ILanguageRouteComponentSource[] { package },
            WistModuleContractRouteObserver.CreateRuntimeOptions(
                runtimeAssemblies,
                CreateModuleContractOptions(optionsSnapshot.VerificationPolicy)));

        return new WistEngine(runtime, plan, backend, optionsSnapshot, resourceLimits);
    }

    public T Evaluate<T>(string code)
    {
        using var operation = EnterOperation();
        EnsureSourceWithinLimits(code);
        var result = Runtime.Run(new LanguageExecutionRequest(code, _backend));
        return WistResultConverter.ConvertTo<T>(result.Value);
    }

    public T Evaluate<T>(string code, object arguments)
    {
        arguments = arguments.ArgNotNull();
        return Evaluate<T>(code, WistArgumentReader.FromObject(arguments));
    }

    public T Evaluate<T>(string code, IReadOnlyDictionary<string, object?> arguments)
    {
        using var operation = EnterOperation();
        EnsureSourceWithinLimits(code);
        arguments = arguments.ArgNotNull();
        EnsureParameterCountWithinLimits(arguments.Count);
        var normalized = NormalizeArguments(arguments);
        var result = Runtime.Run(new LanguageExecutionRequest(code, _backend, normalized));
        return WistResultConverter.ConvertTo<T>(result.Value);
    }

    public WistValidationResult Validate(string code)
    {
        using var operation = EnterOperation();
        try
        {
            EnsureSourceWithinLimits(code);
            var built = Runtime.Build(LanguageArtifactBuildRequest.FromText(code, _backend));
            return WistValidationResult.Success(CreateOptimizationReport(
                WistBuiltArtifactActivation.GetSsaReport(Runtime, built)));
        }
        catch (Exception exception)
        {
            var kind = WistFailureClassifier.Classify(exception);
            if (!WistFailureClassifier.IsStructuredResultFailure(kind))
                throw;
            return WistValidationResult.Failure(
                kind,
                ExposeExpectedException(exception),
                WistDiagnosticFactory.FromException(exception, "Validation", _options.DiagnosticExposure),
                CreateOptimizationReport(WistBuiltArtifactActivation.TryGetSsaReport(exception)));
        }
    }

    public WistValidationResult Validate(string code, object sampleArguments)
    {
        using var operation = EnterOperation();
        try
        {
            EnsureSourceWithinLimits(code);
            sampleArguments = sampleArguments.ArgNotNull();
            var arguments = WistArgumentReader.FromObject(sampleArguments);
            EnsureParameterCountWithinLimits(arguments.Count);
            var bindings = CreateBuildBindings(arguments);
            var built = Runtime.Build(LanguageArtifactBuildRequest.FromText(code, _backend, bindings));
            return WistValidationResult.Success(CreateOptimizationReport(
                WistBuiltArtifactActivation.GetSsaReport(Runtime, built)));
        }
        catch (Exception exception)
        {
            var kind = WistFailureClassifier.Classify(exception);
            if (!WistFailureClassifier.IsStructuredResultFailure(kind))
                throw;
            return WistValidationResult.Failure(
                kind,
                ExposeExpectedException(exception),
                WistDiagnosticFactory.FromException(exception, "Validation", _options.DiagnosticExposure),
                CreateOptimizationReport(WistBuiltArtifactActivation.TryGetSsaReport(exception)));
        }
    }

    public WistProgram<TDelegate> Compile<TDelegate>(string formula, params string[] parameterNames)
        where TDelegate : Delegate
    {
        using var operation = EnterOperation();
        return CompileCore<TDelegate>(formula, parameterNames);
    }

    public WistCompileResult<TDelegate> TryCompile<TDelegate>(string formula, params string[] parameterNames)
        where TDelegate : Delegate
    {
        using var operation = EnterOperation();
        try
        {
            return WistCompileResult<TDelegate>.Success(CompileCore<TDelegate>(formula, parameterNames));
        }
        catch (Exception exception)
        {
            var kind = WistFailureClassifier.Classify(exception);
            if (!WistFailureClassifier.IsStructuredResultFailure(kind))
                throw;
            return WistCompileResult<TDelegate>.Failure(
                kind,
                ExposeExpectedException(exception),
                WistDiagnosticFactory.FromException(exception, "Compilation", _options.DiagnosticExposure),
                CreateOptimizationReport(WistBuiltArtifactActivation.TryGetSsaReport(exception)));
        }
    }

    private WistProgram<TDelegate> CompileCore<TDelegate>(string formula, string[] parameterNames)
        where TDelegate : Delegate
    {
        EnsureSourceWithinLimits(formula);
        parameterNames = parameterNames.ArgNotNull();
        EnsureParameterCountWithinLimits(parameterNames.Length);

        var signature = WistDelegateSignature.FromDelegate<TDelegate>(parameterNames);
        var bindings = signature.BindingTypes
            .Select(binding => LanguageBuildBinding.Declare(
                binding.Key,
                WistRuntimeValueAdapterActivation.NormalizeDeclaredType(_plan, binding.Value)))
            .ToArray();
        var built = Runtime.Build(LanguageArtifactBuildRequest.FromText(formula, _backend, bindings));
        var durableProgram = WistBuiltArtifactActivation.Materialize(Runtime, built);
        var compiledDelegate = WistDurableDelegateFactory.Create<TDelegate>(durableProgram);
        var report = CreateOptimizationReport(WistBuiltArtifactActivation.GetSsaReport(Runtime, built));

        return new WistProgram<TDelegate>(
            compiledDelegate,
            new WistProgramMetadata(
                formula,
                _options.BackendId,
                signature.ParameterNames,
                signature.ParameterTypes,
                signature.ReturnType,
                report,
                _options.SourceRetention));
    }

    private static LanguageDefinition ResolveLanguageDefinition(WistEngineOptions options)
    {
        var policy = options.Optimization.Ssa.Policy switch
        {
            WistSsaPolicy.Disabled => WistFacadeSsaPolicy.Disabled,
            WistSsaPolicy.Prefer => WistFacadeSsaPolicy.Prefer,
            WistSsaPolicy.Require => WistFacadeSsaPolicy.Require,
            WistSsaPolicy.Debug => WistFacadeSsaPolicy.Debug,
            _ => throw new ArgumentOutOfRangeException(nameof(options.Optimization.Ssa.Policy))
        };

        return options.DialectSource switch
        {
            WistDialectSource.ShippedPreset preset =>
                WistFacadeLanguageDefinitionFactory.FromPreset(preset.PresetId, options.BackendId, policy),
            WistDialectSource.File file => FromDialectFile(file, options.BackendId, policy),
            WistDialectSource.Text text => WistFacadeLanguageDefinitionFactory.FromDialectText(
                text.SourceText,
                text.SourceName,
                options.BackendId,
                policy),
            _ => throw new InvalidOperationException("Unsupported Wist dialect source.")
        };
    }

    private static LanguageDefinition FromDialectFile(WistDialectSource.File file, string backend, WistFacadeSsaPolicy policy)
    {
        var path = Path.GetFullPath(file.Path);
        return WistFacadeLanguageDefinitionFactory.FromDialectText(
            File.ReadAllText(path),
            Path.GetFileName(path),
            backend,
            policy);
    }

    private IReadOnlyDictionary<string, object?> NormalizeArguments(IReadOnlyDictionary<string, object?> arguments)
    {
        var normalized = new Dictionary<string, object?>(arguments.Count, StringComparer.Ordinal);
        foreach (var argument in arguments)
            normalized.Add(argument.Key, WistRuntimeValueAdapterActivation.NormalizeInput(_plan, argument.Value));
        return normalized;
    }

    private IReadOnlyList<LanguageBuildBinding> CreateBuildBindings(IReadOnlyDictionary<string, object?> arguments)
    {
        var bindings = new List<LanguageBuildBinding>(arguments.Count);
        foreach (var argument in arguments)
        {
            var normalizedValue = WistRuntimeValueAdapterActivation.NormalizeInput(_plan, argument.Value);
            var publicType = argument.Value?.GetType() ?? typeof(object);
            var declaredType = WistRuntimeValueAdapterActivation.NormalizeDeclaredType(_plan, publicType);
            bindings.Add(LanguageBuildBinding.Create(argument.Key, declaredType, normalizedValue));
        }
        return bindings;
    }

    private WistOptimizationReport CreateOptimizationReport(WistSsaReportSnapshot? report)
    {
        var requested = _options.Optimization.Ssa.Policy;
        if (report == null)
        {
            if (requested == WistSsaPolicy.Disabled)
                return WistOptimizationReport.Disabled;

            return new WistOptimizationReport(
                new WistSsaOptimizationReport(
                    requested,
                    usedSsa: false,
                    fellBackToAir: false,
                    profile: null,
                    inputAirInstructionCount: 0,
                    outputAirInstructionCount: 0,
                    diagnostics:
                    [
                        new WistSsaRouteDiagnostic(
                            "wist.ssa.report.missing",
                            "The SSA optimizer was requested, but no route report was published before the operation completed.",
                            "route")
                    ]));
        }

        var exposeTrace = requested == WistSsaPolicy.Debug ||
                          _options.Optimization.Ssa.DiagnosticLevel == WistSsaDiagnosticLevel.Detailed;
        return new WistOptimizationReport(
            new WistSsaOptimizationReport(
                requested,
                report.UsedSsa,
                report.FellBackToInput,
                report.ProfileId,
                report.InputAirInstructionCount,
                report.OutputAirInstructionCount,
                report.ExecutedPasses,
                report.Diagnostics.Select(static diagnostic =>
                    new WistSsaRouteDiagnostic(diagnostic.Code, diagnostic.Message, diagnostic.Stage)),
                exposeTrace
                    ? report.Trace.Select(static entry =>
                        new WistSsaTraceEntry(entry.Stage, entry.Message, entry.InstructionCount))
                    : []));
    }

    private IDisposable EnterOperation()
    {
        var lifetime = _lifetime.EnterOperation(this);
        try
        {
            var concurrency = _concurrency.Enter();
            return new CompositeOperationLease(concurrency, lifetime);
        }
        catch
        {
            lifetime.Dispose();
            throw;
        }
    }

    private Exception? ExposeExpectedException(Exception exception) =>
        _options.DiagnosticExposure == WistDiagnosticExposure.Developer ? exception : null;

    private static ModuleContractVerificationOptions CreateModuleContractOptions(WistVerificationPolicy policy) =>
        new ModuleContractVerificationOptions
        {
            Mode = ModuleContractVerificationMode.Strict,
            PipelineOptions = ModuleContractPipelineProfiles.StrictEnforced with
            {
                VerificationPolicy = policy switch
                {
                    WistVerificationPolicy.P0Structural => ModuleContractVerificationPolicy.P0Structural,
                    WistVerificationPolicy.P1Invalidation => ModuleContractVerificationPolicy.P1Invalidation,
                    WistVerificationPolicy.P2Selective => ModuleContractVerificationPolicy.P2Selective,
                    WistVerificationPolicy.P3Always => ModuleContractVerificationPolicy.P3Always,
                    _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown Wist verification policy.")
                }
            },
            DiagnosticSink = new InMemoryModuleContractDiagnosticSink()
        }.SnapshotValidated();

    private static WistVerificationPolicy RequireVerificationPolicy(WistVerificationPolicy policy) =>
        Enum.IsDefined(policy)
            ? policy
            : throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown Wist verification policy.");

    private static WistSourceRetentionPolicy RequireSourceRetention(WistSourceRetentionPolicy policy) =>
        Enum.IsDefined(policy)
            ? policy
            : throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown Wist source-retention policy.");

    private static WistDiagnosticExposure RequireDiagnosticExposure(WistDiagnosticExposure exposure) =>
        Enum.IsDefined(exposure)
            ? exposure
            : throw new ArgumentOutOfRangeException(nameof(exposure), exposure, "Unknown Wist diagnostic exposure.");

    private static string RequireBackendId(string backendId)
    {
        if (string.Equals(backendId, "cil", StringComparison.Ordinal) ||
            string.Equals(backendId, "interpreter", StringComparison.Ordinal))
            return backendId;

        return Thrower.ArgumentOutOfRange<string>(
            nameof(backendId),
            $"Unsupported Wist backend '{backendId}'. Expected 'cil' or 'interpreter'.");
    }

    private void EnsureSourceWithinLimits(string code)
    {
        code = code.ArgNotNull();
        if (code.Length <= _resourceLimits.MaxSourceLength)
            return;

        throw new WistResourceLimitException(
            WistDiagnosticCodes.SourceLimitExceeded,
            $"Wist source length {code.Length} exceeds the configured maximum of {_resourceLimits.MaxSourceLength} UTF-16 code units.");
    }

    private void EnsureParameterCountWithinLimits(int count)
    {
        if (count <= _resourceLimits.MaxParameterCount)
            return;

        throw new WistResourceLimitException(
            WistDiagnosticCodes.ParameterLimitExceeded,
            $"Wist parameter count {count} exceeds the configured maximum of {_resourceLimits.MaxParameterCount}.");
    }

    private LanguageBuildRuntime Runtime =>
        Volatile.Read(ref _runtime)
        ?? throw new ObjectDisposedException(nameof(WistEngine));

    private sealed class CompositeOperationLease(IDisposable concurrency, IDisposable lifetime) : IDisposable
    {
        private IDisposable? _concurrency = concurrency;
        private IDisposable? _lifetime = lifetime;

        public void Dispose()
        {
            Interlocked.Exchange(ref _concurrency, null)?.Dispose();
            Interlocked.Exchange(ref _lifetime, null)?.Dispose();
        }
    }
}
