using System.Collections.ObjectModel;

namespace UniversalToolchain.Wist;

/// <summary>
/// Facade-owned report describing optional optimization routes used for one operation.
/// </summary>
public sealed class WistOptimizationReport
{
    public WistOptimizationReport(WistSsaOptimizationReport ssa)
    {
        Ssa = ssa ?? throw new ArgumentNullException(nameof(ssa));
    }

    public WistSsaOptimizationReport Ssa { get; }

    internal static WistOptimizationReport Disabled { get; } = new(WistSsaOptimizationReport.Disabled);
}

/// <summary>
/// Observable result of the experimental SSA route without exposing low-level SSA assemblies.
/// </summary>
public sealed class WistSsaOptimizationReport
{
    public WistSsaOptimizationReport(
        WistSsaPolicy requestedPolicy,
        bool usedSsa,
        bool fellBackToAir,
        string? profile,
        int inputAirInstructionCount,
        int outputAirInstructionCount,
        IEnumerable<string>? executedPasses = null,
        IEnumerable<WistSsaRouteDiagnostic>? diagnostics = null,
        IEnumerable<WistSsaTraceEntry>? trace = null)
    {
        if (!Enum.IsDefined(requestedPolicy))
            throw new ArgumentOutOfRangeException(nameof(requestedPolicy));
        if (inputAirInstructionCount < 0)
            throw new ArgumentOutOfRangeException(nameof(inputAirInstructionCount));
        if (outputAirInstructionCount < 0)
            throw new ArgumentOutOfRangeException(nameof(outputAirInstructionCount));
        if (usedSsa && fellBackToAir)
            throw new ArgumentException("An SSA route cannot both succeed and fall back to AIR.");
        if (requestedPolicy == WistSsaPolicy.Disabled && (usedSsa || fellBackToAir))
            throw new ArgumentException("A disabled SSA route cannot be reported as used or fallen back.");
        if (fellBackToAir && requestedPolicy != WistSsaPolicy.Prefer)
            throw new ArgumentException("Only the Prefer SSA policy may fall back to AIR.");
        if ((usedSsa || fellBackToAir) && string.IsNullOrWhiteSpace(profile))
            throw new ArgumentException("An attempted SSA route must identify its profile.", nameof(profile));

        RequestedPolicy = requestedPolicy;
        UsedSsa = usedSsa;
        FellBackToAir = fellBackToAir;
        Profile = string.IsNullOrWhiteSpace(profile) ? null : profile.Trim();
        InputAirInstructionCount = inputAirInstructionCount;
        OutputAirInstructionCount = outputAirInstructionCount;
        ExecutedPasses = new ReadOnlyCollection<string>((executedPasses ?? []).ToArray());
        Diagnostics = new ReadOnlyCollection<WistSsaRouteDiagnostic>((diagnostics ?? []).ToArray());
        Trace = new ReadOnlyCollection<WistSsaTraceEntry>((trace ?? []).ToArray());
    }

    public WistSsaPolicy RequestedPolicy { get; }

    public bool UsedSsa { get; }

    public bool FellBackToAir { get; }

    public string? Profile { get; }

    public int InputAirInstructionCount { get; }

    public int OutputAirInstructionCount { get; }

    public IReadOnlyList<string> ExecutedPasses { get; }

    public IReadOnlyList<WistSsaRouteDiagnostic> Diagnostics { get; }

    public IReadOnlyList<WistSsaTraceEntry> Trace { get; }

    internal static WistSsaOptimizationReport Disabled { get; } = new(
        WistSsaPolicy.Disabled,
        usedSsa: false,
        fellBackToAir: false,
        profile: null,
        inputAirInstructionCount: 0,
        outputAirInstructionCount: 0);
}

public sealed record WistSsaRouteDiagnostic(string Code, string Message);

public sealed record WistSsaTraceEntry(string Stage, string Message, int? InstructionCount);
