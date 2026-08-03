using System.Threading;
using UniversalToolchain.Ir.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

/// <summary>
/// Stable host/runtime contract defaults for the SSA route.
/// </summary>
public static class SsaRuntimeExecutionDefaults
{
    public const string ProfileId = "preview-int32-managed";
}

public enum SsaDiagnosticMode
{
    Default,
    Verbose
}

public enum SsaRoutePolicy
{
    Off,
    Prefer,
    Require,
    Debug
}

public sealed record SsaRouteDiagnostic(string Code, string Message)
{
    public SsaRouteDiagnostic(string code, string message, string? stage)
        : this(code, message)
    {
        Stage = string.IsNullOrWhiteSpace(stage) ? null : stage.Trim();
    }

    public string? Stage { get; init; }
}

public sealed record SsaRouteTraceEntry(string Stage, string Message, int? InstructionCount = null);

public sealed class SsaRouteReport
{
    public SsaRouteReport(
        SsaRoutePolicy policy,
        string profileId,
        bool usedSsa,
        bool fellBackToInput,
        int inputAirInstructionCount,
        int outputAirInstructionCount,
        IEnumerable<string>? executedPasses = null,
        IEnumerable<SsaRouteDiagnostic>? diagnostics = null,
        IEnumerable<SsaRouteTraceEntry>? trace = null)
    {
        if (!Enum.IsDefined(policy))
            throw new ArgumentOutOfRangeException(nameof(policy), policy, "SSA route policy is not defined.");
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("SSA profile identifier must not be empty.", nameof(profileId));
        if (inputAirInstructionCount < 0)
            throw new ArgumentOutOfRangeException(nameof(inputAirInstructionCount));
        if (outputAirInstructionCount < 0)
            throw new ArgumentOutOfRangeException(nameof(outputAirInstructionCount));
        if (usedSsa && fellBackToInput)
            throw new ArgumentException("An SSA route cannot both succeed and fall back to its input.");
        if (policy == SsaRoutePolicy.Off && (usedSsa || fellBackToInput))
            throw new ArgumentException("A disabled SSA route cannot be reported as used or fallen back.");
        if (fellBackToInput && policy != SsaRoutePolicy.Prefer)
            throw new ArgumentException("Only the Prefer SSA policy may fall back to its input.");
        if (fellBackToInput && outputAirInstructionCount != inputAirInstructionCount)
        {
            throw new ArgumentException(
                "An SSA fallback must preserve the input AIR instruction count.",
                nameof(outputAirInstructionCount));
        }

        Policy = policy;
        ProfileId = profileId.Trim();
        UsedSsa = usedSsa;
        FellBackToInput = fellBackToInput;
        InputAirInstructionCount = inputAirInstructionCount;
        OutputAirInstructionCount = outputAirInstructionCount;
        ExecutedPasses = (executedPasses ?? []).ToArray();
        Diagnostics = (diagnostics ?? []).ToArray();
        Trace = (trace ?? []).ToArray();
    }

    public SsaRoutePolicy Policy { get; }

    public string ProfileId { get; }

    public bool UsedSsa { get; }

    public bool FellBackToInput { get; }

    public int InputAirInstructionCount { get; }

    public int OutputAirInstructionCount { get; }

    public IReadOnlyList<string> ExecutedPasses { get; }

    public IReadOnlyList<SsaRouteDiagnostic> Diagnostics { get; }

    public IReadOnlyList<SsaRouteTraceEntry> Trace { get; }
}

/// <summary>
/// Immutable runtime configuration for the alpha SSA optimizer module.
/// </summary>
public sealed class SsaRuntimeExecutionOptions
{
    public static SsaRuntimeExecutionOptions RequireDefault { get; } = new();

    public SsaRoutePolicy Policy { get; init; } = SsaRoutePolicy.Require;

    public SsaDiagnosticMode Diagnostics { get; init; } = SsaDiagnosticMode.Default;

    public CapabilitySet TargetCapabilities { get; init; } = CapabilitySet.Empty;

    public string ProfileId { get; init; } = SsaRuntimeExecutionDefaults.ProfileId;

    public SsaRuntimeExecutionOptions SnapshotValidated()
    {
        if (!Enum.IsDefined(Policy))
            throw new ArgumentOutOfRangeException(nameof(Policy), Policy, "SSA route policy is not defined.");
        if (!Enum.IsDefined(Diagnostics))
            throw new ArgumentOutOfRangeException(nameof(Diagnostics), Diagnostics, "SSA diagnostic mode is not defined.");
        if (string.IsNullOrWhiteSpace(ProfileId))
            throw new ArgumentException("SSA profile identifier must not be empty.", nameof(ProfileId));

        return new SsaRuntimeExecutionOptions
        {
            Policy = Policy,
            Diagnostics = Diagnostics,
            TargetCapabilities = new CapabilitySet((TargetCapabilities ?? CapabilitySet.Empty).Values),
            ProfileId = ProfileId.Trim()
        };
    }
}

public interface ISsaRouteReportSink
{
    void Publish(SsaRouteReport report);
}

public sealed class NullSsaRouteReportSink : ISsaRouteReportSink
{
    public static NullSsaRouteReportSink Instance { get; } = new();

    private NullSsaRouteReportSink()
    {
    }

    public void Publish(SsaRouteReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
    }
}

/// <summary>
/// Captures a route report for the current async execution flow without process-wide mutable result state.
/// </summary>
public sealed class SsaRouteReportCollector : ISsaRouteReportSink
{
    private readonly AsyncLocal<CaptureFrame?> _current = new();

    public Capture BeginCapture()
    {
        var frame = new CaptureFrame(_current.Value);
        _current.Value = frame;
        return new Capture(this, frame);
    }

    public void Publish(SsaRouteReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (_current.Value is { } frame)
            frame.Report = report;
    }

    public sealed class Capture : IDisposable
    {
        private readonly SsaRouteReportCollector _owner;
        private readonly CaptureFrame _frame;
        private bool _disposed;

        internal Capture(SsaRouteReportCollector owner, CaptureFrame frame)
        {
            _owner = owner;
            _frame = frame;
        }

        public SsaRouteReport? Report => _frame.Report;

        public void Dispose()
        {
            if (_disposed)
                return;

            if (ReferenceEquals(_owner._current.Value, _frame))
                _owner._current.Value = _frame.Previous;

            _disposed = true;
        }
    }

    internal sealed class CaptureFrame(CaptureFrame? previous)
    {
        public CaptureFrame? Previous { get; } = previous;

        public SsaRouteReport? Report { get; set; }
    }
}
