using System.Threading;
using UniversalToolchain.Ir.Abstractions;

namespace UniversalToolchain.Ssa.Optimization;

/// <summary>
/// Immutable runtime configuration for the preview SSA optimizer module.
/// </summary>
public sealed class SsaRuntimeExecutionOptions
{
    public static SsaRuntimeExecutionOptions RequireDefault { get; } = new();

    public SsaRoutePolicy Policy { get; init; } = SsaRoutePolicy.Require;

    public SsaDiagnosticMode Diagnostics { get; init; } = SsaDiagnosticMode.Default;

    public CapabilitySet TargetCapabilities { get; init; } = CapabilitySet.Empty;

    public string ProfileId { get; init; } = SsaPreviewRouteProfiles.ProfileId;

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
