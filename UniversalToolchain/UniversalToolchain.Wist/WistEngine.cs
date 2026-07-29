using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Presets;
using UniversalToolchain.Ssa.Optimization;

namespace UniversalToolchain.Wist;

/// <summary>
/// Public Wist facade for convenient evaluation and typed compiled functions.
/// </summary>
public sealed class WistEngine : IDisposable
{
    private WistDialectExecutionHost? _host;
    private readonly WistEngineOptions _options;
    private readonly IWistDelegateCompiler _delegateCompiler;
    private readonly WistResourceLimits _resourceLimits;
    private WistRuntimeBoundary? _runtimeBoundary;
    private readonly SsaRouteReportCollector _ssaReportCollector;
    private bool _disposed;

    private WistEngine(
        WistDialectExecutionHost host,
        WistEngineOptions options,
        IWistDelegateCompiler delegateCompiler,
        WistResourceLimits resourceLimits,
        WistRuntimeBoundary runtimeBoundary,
        SsaRouteReportCollector ssaReportCollector)
    {
        _host = host;
        _options = options;
        _delegateCompiler = delegateCompiler;
        _resourceLimits = resourceLimits;
        _runtimeBoundary = runtimeBoundary;
        _ssaReportCollector = ssaReportCollector;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var host = _host;
        _host = null;
        _runtimeBoundary = null;
        host?.Dispose();
    }

    public static WistEngine CreateRestrictedArithmetic() =>
        Create(WistEngineOptions.FromPresetId("pricing-restricted"));

    public static WistEngine CreateFullNative() =>
        Create(WistEngineOptions.FromPresetId("full-default-native"));

    /// <summary>
    /// Creates a Wist engine from public facade options. Options are snapshotted at creation time.
    /// </summary>
    public static WistEngine Create(WistEngineOptions options)
    {
        options = options.ArgNotNull();
        var resourceLimits = options.ResourceLimits.ArgNotNull().SnapshotValidated();
        var optimization = options.Optimization.ArgNotNull().SnapshotValidated();
        var allowedAssemblies = options.AllowedAssemblies.ArgNotNull().ToArray();

        if (allowedAssemblies.Any(static assembly => assembly is null))
            Thrower.Argument(nameof(options.AllowedAssemblies), "The allowed assembly collection must not contain null values.");

        var optionsSnapshot = new WistEngineOptions
        {
            DialectSource = options.DialectSource.ArgNotNull(),
            BackendId = RequireBackendId(options.BackendId),
            AllowedAssemblies = allowedAssemblies,
            ResourceLimits = resourceLimits,
            Optimization = optimization
        };

        var services = new ServiceCollection();
        services.AddWistDialectServices();

        ServiceProvider? compositionProvider = services.BuildServiceProvider();
        try
        {
            var workflow = compositionProvider.GetRequiredService<WistDialectExecutionWorkflow>();
            var source = ResolveDialectSource(optionsSnapshot);
            var composition = Compose(workflow, source, optimization.Ssa);

            if (!composition.IsSuccess)
            {
                Thrower.InvalidOpEx(
                    DialectCompositionExplanationFormatter.FormatDeterministic(
                        DialectCompositionExplanationProjector.Project(composition)));
            }

            var reportCollector = new SsaRouteReportCollector();
            var runtimeServiceOptions = new WistRuntimeServiceOptions
            {
                AllowedAssemblies = allowedAssemblies,
                SsaExecution = CreateSsaExecutionOptions(optimization.Ssa),
                SsaReportSink = reportCollector
            };

            var compositionOwner = compositionProvider;
            compositionProvider = null;
            var host = workflow.CreateHost(composition, runtimeServiceOptions, compositionOwner);
            try
            {
                EnsureBackendEnabled(host.Configuration, optionsSnapshot.BackendId);
                return new WistEngine(
                    host,
                    optionsSnapshot,
                    new WistBackendDelegateCompiler(),
                    resourceLimits,
                    WistRuntimeBoundary.Create(host.Configuration),
                    reportCollector);
            }
            catch
            {
                host.Dispose();
                throw;
            }
        }
        finally
        {
            compositionProvider?.Dispose();
        }
    }

    public T Evaluate<T>(string code)
    {
        ThrowIfDisposed();
        EnsureSourceWithinLimits(code);
        return WistResultConverter.ConvertTo<T>(Host.Run(code, _options.BackendId));
    }

    public T Evaluate<T>(string code, object arguments)
    {
        arguments = arguments.ArgNotNull();
        return Evaluate<T>(code, WistArgumentReader.FromObject(arguments));
    }

    public T Evaluate<T>(string code, IReadOnlyDictionary<string, object?> arguments)
    {
        ThrowIfDisposed();
        EnsureSourceWithinLimits(code);
        arguments = arguments.ArgNotNull();
        EnsureParameterCountWithinLimits(arguments.Count);
        var normalizedArguments = RuntimeBoundary.NormalizeArguments(arguments);
        return WistResultConverter.ConvertTo<T>(Host.Run(code, normalizedArguments, _options.BackendId));
    }

    public WistValidationResult Validate(string code)
    {
        ThrowIfDisposed();
        using var capture = _ssaReportCollector.BeginCapture();

        try
        {
            EnsureSourceWithinLimits(code);
            _ = Host.Compile(code, null, _options.BackendId);
            return WistValidationResult.Success(CreateOptimizationReport(capture.Report));
        }
        catch (Exception exception)
        {
            return WistValidationResult.Failure(
                exception,
                WistDiagnosticFactory.FromException(exception, "Validation"),
                CreateOptimizationReport(capture.Report));
        }
    }

    public WistValidationResult Validate(string code, object sampleArguments)
    {
        ThrowIfDisposed();
        using var capture = _ssaReportCollector.BeginCapture();

        try
        {
            EnsureSourceWithinLimits(code);
            sampleArguments = sampleArguments.ArgNotNull();
            var argumentTypes = RuntimeBoundary.NormalizeArguments(WistArgumentReader.FromObject(sampleArguments));
            EnsureParameterCountWithinLimits(argumentTypes.Count);
            var declaredBindings = CreateDeclaredBindings(argumentTypes);
            _ = Host.Compile(code, declaredBindings, _options.BackendId);
            return WistValidationResult.Success(CreateOptimizationReport(capture.Report));
        }
        catch (Exception exception)
        {
            return WistValidationResult.Failure(
                exception,
                WistDiagnosticFactory.FromException(exception, "Validation"),
                CreateOptimizationReport(capture.Report));
        }
    }

    public WistProgram<TDelegate> Compile<TDelegate>(string formula, params string[] parameterNames)
        where TDelegate : Delegate
    {
        ThrowIfDisposed();
        using var capture = _ssaReportCollector.BeginCapture();
        return CompileCore<TDelegate>(formula, parameterNames, () => CreateOptimizationReport(capture.Report));
    }

    public WistCompileResult<TDelegate> TryCompile<TDelegate>(string formula, params string[] parameterNames)
        where TDelegate : Delegate
    {
        ThrowIfDisposed();
        using var capture = _ssaReportCollector.BeginCapture();

        try
        {
            var program = CompileCore<TDelegate>(formula, parameterNames, () => CreateOptimizationReport(capture.Report));
            return WistCompileResult<TDelegate>.Success(program);
        }
        catch (Exception exception)
        {
            return WistCompileResult<TDelegate>.Failure(
                exception,
                WistDiagnosticFactory.FromException(exception, "Compilation"),
                CreateOptimizationReport(capture.Report));
        }
    }




    private WistProgram<TDelegate> CompileCore<TDelegate>(
        string formula,
        string[] parameterNames,
        Func<WistOptimizationReport> reportFactory)
        where TDelegate : Delegate
    {
        EnsureSourceWithinLimits(formula);
        parameterNames = parameterNames.ArgNotNull();
        EnsureParameterCountWithinLimits(parameterNames.Length);

        var signature = WistDelegateSignature.FromDelegate<TDelegate>(parameterNames);
        var compiledDelegate = _delegateCompiler.CompileDelegate<TDelegate>(
            Host,
            formula,
            CreateDeclaredBindings(signature.BindingTypes, RuntimeBoundary.NormalizeDeclaredType),
            _options.BackendId,
            RuntimeBoundary);

        return new WistProgram<TDelegate>(
            compiledDelegate,
            new WistProgramMetadata(
                formula,
                _options.BackendId,
                signature.ParameterNames,
                signature.ParameterTypes,
                signature.ReturnType,
                reportFactory()));
    }

    private static DialectFrameworkCompositionResult Compose(
        WistDialectExecutionWorkflow workflow,
        ResolvedDialectSource source,
        WistSsaOptions ssa)
    {
        if (ssa.Policy == WistSsaPolicy.Disabled)
            return workflow.ComposeText(source.SourceText, source.SourceName);

        var profile = RuntimeProfileDefinitionBuilder
            .Create("wist-public-ssa")
            .Describe("Enables the experimental verifier-gated SSA route selected by WistEngineOptions.")
            .EnableOptimizer("Ssa")
            .Build();
        return workflow.ComposeText(
            source.SourceText,
            source.SourceName,
            profile,
            RuntimeProfileOverridePolicy.StrictNoConflicts);
    }

    private static SsaRuntimeExecutionOptions CreateSsaExecutionOptions(WistSsaOptions options) => new()
    {
        Policy = options.Policy switch
        {
            WistSsaPolicy.Disabled => SsaRoutePolicy.Off,
            WistSsaPolicy.Prefer => SsaRoutePolicy.Prefer,
            WistSsaPolicy.Require => SsaRoutePolicy.Require,
            WistSsaPolicy.Debug => SsaRoutePolicy.Debug,
            _ => throw new ArgumentOutOfRangeException(nameof(options.Policy))
        },
        Diagnostics = options.DiagnosticLevel == WistSsaDiagnosticLevel.Detailed
            ? SsaDiagnosticMode.Verbose
            : SsaDiagnosticMode.Default,
        ProfileId = SsaRouteProfiles.ProfileId
    };

    private WistOptimizationReport CreateOptimizationReport(SsaRouteReport? report)
    {
        var requested = _options.Optimization.Ssa.Policy;
        if (report is null)
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
                    new WistSsaRouteDiagnostic(
                        diagnostic.Code,
                        diagnostic.Message,
                        diagnostic.Stage)),
                report.Trace.Select(static entry =>
                    new WistSsaTraceEntry(entry.Stage, entry.Message, entry.InstructionCount))));
    }

    private static ResolvedDialectSource ResolveDialectSource(WistEngineOptions options)
    {
        var resolver = new WistShippedDialectFileResolver();
        return options.DialectSource switch
        {
            WistDialectSource.File file => ReadDialectFile(Path.GetFullPath(file.Path)),
            WistDialectSource.ShippedPreset preset =>
                ReadDialectFile(resolver.Resolve(WistShippedDialectPresets.GetRequired(preset.PresetId))),
            WistDialectSource.Text text => new ResolvedDialectSource(text.SourceText, text.SourceName),
            _ => Thrower.InvalidOpEx<ResolvedDialectSource>("Unsupported Wist dialect source.")
        };
    }

    private static ResolvedDialectSource ReadDialectFile(string path) =>
        new(File.ReadAllText(path), Path.GetFileName(path));

    private static void EnsureBackendEnabled(
        ToolchainRuntimeConfiguration configuration,
        string backend)
    {
        if (configuration.TryResolveKnownBackendId(backend, out var backendId) &&
            configuration.TryGetEnabledBackend(backendId, out _))
        {
            return;
        }

        var enabled = string.Join(
            ", ",
            configuration.EnabledBackends
                .Select(static descriptor => descriptor.CanonicalId)
                .OrderBy(static id => id, StringComparer.Ordinal));
        Thrower.ArgumentOutOfRange<object>(
            nameof(WistEngineOptions.BackendId),
            $"Dialect '{configuration.DialectName}' does not enable backend '{backend}'. Enabled backends: {enabled}.");
    }

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

    private WistDialectExecutionHost Host
    {
        get
        {
            ThrowIfDisposed();
            return _host!;
        }
    }

    private WistRuntimeBoundary RuntimeBoundary
    {
        get
        {
            ThrowIfDisposed();
            return _runtimeBoundary!;
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private static OrderedDictionary<string, Type> CreateDeclaredBindings(IReadOnlyDictionary<string, object?> arguments)
    {
        var bindings = new OrderedDictionary<string, Type>();
        foreach (var argument in arguments)
            bindings[argument.Key] = argument.Value?.GetType() ?? typeof(object);

        return bindings;
    }

    private static OrderedDictionary<string, Type> CreateDeclaredBindings(
        IReadOnlyDictionary<string, Type> bindingTypes,
        Func<Type, Type>? typeNormalizer = null)
    {
        var bindings = new OrderedDictionary<string, Type>();
        foreach (var binding in bindingTypes)
            bindings[binding.Key] = typeNormalizer?.Invoke(binding.Value) ?? binding.Value;

        return bindings;
    }

    private sealed record ResolvedDialectSource(string SourceText, string SourceName);
}
